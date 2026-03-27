using JobFlow.API.Models;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public sealed class ClientImportProcessor
{
    private const int MaxPersistedErrors = 1000;

    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;
    private readonly ClientImportUploadSessionService _uploadSessionService;
    private readonly ILogger<ClientImportProcessor> _logger;

    public ClientImportProcessor(
        IDbContextFactory<JobFlowDbContext> dbContextFactory,
        ClientImportUploadSessionService uploadSessionService,
        ILogger<ClientImportProcessor> logger)
    {
        _dbContextFactory = dbContextFactory;
        _uploadSessionService = uploadSessionService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid jobId,
        Guid organizationId,
        Guid uploadSessionId,
        Dictionary<string, string?> columnMappings)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var importJob = await dbContext.Set<ClientImportJob>()
            .FirstOrDefaultAsync(x => x.Id == jobId && x.OrganizationId == organizationId);

        if (importJob is null)
        {
            return;
        }

        importJob.Status = "running";
        importJob.StartedAtUtc ??= DateTime.UtcNow;
        importJob.ErrorMessage = null;
        await dbContext.SaveChangesAsync();

        try
        {
            var session = await _uploadSessionService.GetActiveSessionAsync(uploadSessionId, organizationId, CancellationToken.None);
            if (session is null)
            {
                await MarkFailedAsync(dbContext, importJob, "Import session not found or expired. Please upload your CSV again.");
                return;
            }

            var clients = dbContext.Set<OrganizationClient>();
            var errors = dbContext.Set<ClientImportJobError>();

            var emailSourceColumn = columnMappings
                .FirstOrDefault(x => string.Equals(x.Value, ClientImportTargetFields.EmailAddress, StringComparison.OrdinalIgnoreCase))
                .Key;

            var sessionEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(emailSourceColumn))
            {
                foreach (var row in session.Rows)
                {
                    if (row.Row.TryGetValue(emailSourceColumn, out var email) && !string.IsNullOrWhiteSpace(email))
                    {
                        sessionEmails.Add(email.Trim().ToLowerInvariant());
                    }
                }
            }

            var existingByEmail = new Dictionary<string, OrganizationClient>(StringComparer.OrdinalIgnoreCase);
            if (sessionEmails.Count > 0)
            {
                var existing = await clients
                    .Where(c => c.OrganizationId == organizationId
                                && c.EmailAddress != null
                                && sessionEmails.Contains(c.EmailAddress.ToLower()))
                    .ToListAsync();

                foreach (var row in existing)
                {
                    if (!string.IsNullOrWhiteSpace(row.EmailAddress))
                    {
                        existingByEmail[row.EmailAddress.Trim().ToLowerInvariant()] = row;
                    }
                }
            }

            var processedRows = 0;
            var succeededRows = 0;
            var failedRows = 0;
            var persistedErrors = 0;

            for (var index = 0; index < session.Rows.Count; index++)
            {
                var rowItem = session.Rows[index];
                var rowNumber = rowItem.RowNumber;
                var row = rowItem.Row;
                processedRows++;

                try
                {
                    var mapped = MapRow(row, columnMappings);

                    if (string.IsNullOrWhiteSpace(mapped.FirstName)
                        && string.IsNullOrWhiteSpace(mapped.LastName)
                        && string.IsNullOrWhiteSpace(mapped.EmailAddress)
                        && string.IsNullOrWhiteSpace(mapped.PhoneNumber))
                    {
                        failedRows++;
                        if (persistedErrors < MaxPersistedErrors)
                        {
                            errors.Add(new ClientImportJobError
                            {
                                Id = Guid.NewGuid(),
                                ClientImportJobId = jobId,
                                RowNumber = rowNumber,
                                Message = "Row has no usable client data.",
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            });
                            persistedErrors++;
                        }
                        continue;
                    }

                    OrganizationClient entity;
                    if (!string.IsNullOrWhiteSpace(mapped.EmailAddress)
                        && existingByEmail.TryGetValue(mapped.EmailAddress.Trim().ToLowerInvariant(), out var existingClient))
                    {
                        entity = existingClient;
                    }
                    else
                    {
                        entity = new OrganizationClient
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = organizationId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await clients.AddAsync(entity);
                    }

                    Merge(entity, mapped);

                    if (!string.IsNullOrWhiteSpace(entity.EmailAddress))
                    {
                        existingByEmail[entity.EmailAddress.Trim().ToLowerInvariant()] = entity;
                    }

                    succeededRows++;
                }
                catch (Exception ex)
                {
                    failedRows++;
                    if (persistedErrors < MaxPersistedErrors)
                    {
                        errors.Add(new ClientImportJobError
                        {
                            Id = Guid.NewGuid(),
                            ClientImportJobId = jobId,
                            RowNumber = rowNumber,
                            Message = Truncate(ex.Message, 2000),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        });
                        persistedErrors++;
                    }

                    _logger.LogWarning(ex, "Client import row failed. JobId={JobId}, Row={RowNumber}", jobId, rowNumber);
                }

                if (processedRows % 200 == 0)
                {
                    importJob.ProcessedRows = processedRows;
                    importJob.SucceededRows = succeededRows;
                    importJob.FailedRows = failedRows;
                    await dbContext.SaveChangesAsync();
                }
            }

            importJob.Status = "completed";
            importJob.ProcessedRows = processedRows;
            importJob.SucceededRows = succeededRows;
            importJob.FailedRows = failedRows;
            importJob.CompletedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await _uploadSessionService.MarkConsumedAsync(uploadSessionId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client import job failed. JobId={JobId}", jobId);
            await MarkFailedAsync(dbContext, importJob, "Import failed unexpectedly. Please retry or contact support.");
        }
    }

    private static async Task MarkFailedAsync(JobFlowDbContext dbContext, ClientImportJob importJob, string message)
    {
        importJob.Status = "failed";
        importJob.ErrorMessage = Truncate(message, 2000);
        importJob.CompletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }

    private static void Merge(OrganizationClient entity, MappedClientRow mapped)
    {
        entity.FirstName = Prefer(mapped.FirstName, entity.FirstName);
        entity.LastName = Prefer(mapped.LastName, entity.LastName);
        entity.EmailAddress = Prefer(mapped.EmailAddress, entity.EmailAddress);
        entity.PhoneNumber = Prefer(mapped.PhoneNumber, entity.PhoneNumber);
        entity.Address1 = Prefer(mapped.Address1, entity.Address1);
        entity.Address2 = Prefer(mapped.Address2, entity.Address2);
        entity.City = Prefer(mapped.City, entity.City);
        entity.State = Prefer(mapped.State, entity.State);
        entity.ZipCode = Prefer(mapped.ZipCode, entity.ZipCode);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = true;
        entity.DeactivatedAtUtc = null;
    }

    private static string? Prefer(string? incoming, string? current)
    {
        return string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();
    }

    private static MappedClientRow MapRow(
        Dictionary<string, string?> row,
        Dictionary<string, string?> columnMappings)
    {
        var mapped = new MappedClientRow();

        foreach (var (sourceColumn, targetField) in columnMappings)
        {
            if (string.IsNullOrWhiteSpace(targetField)
                || string.Equals(targetField, ClientImportTargetFields.Ignore, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!row.TryGetValue(sourceColumn, out var rawValue))
            {
                continue;
            }

            var value = rawValue?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            switch (targetField)
            {
                case ClientImportTargetFields.FirstName:
                    mapped.FirstName = value;
                    break;
                case ClientImportTargetFields.LastName:
                    mapped.LastName = value;
                    break;
                case ClientImportTargetFields.FullName:
                    ApplyFullName(mapped, value);
                    break;
                case ClientImportTargetFields.EmailAddress:
                    mapped.EmailAddress = value;
                    break;
                case ClientImportTargetFields.PhoneNumber:
                    mapped.PhoneNumber = value;
                    break;
                case ClientImportTargetFields.Address1:
                    mapped.Address1 = value;
                    break;
                case ClientImportTargetFields.Address2:
                    mapped.Address2 = value;
                    break;
                case ClientImportTargetFields.City:
                    mapped.City = value;
                    break;
                case ClientImportTargetFields.State:
                    mapped.State = value;
                    break;
                case ClientImportTargetFields.ZipCode:
                    mapped.ZipCode = value;
                    break;
            }
        }

        return mapped;
    }

    private static void ApplyFullName(MappedClientRow mapped, string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return;
        }

        mapped.FirstName ??= parts[0];

        if (parts.Length > 1)
        {
            mapped.LastName ??= string.Join(' ', parts.Skip(1));
        }
    }

    private sealed class MappedClientRow
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
    }
}
