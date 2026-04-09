using JobFlow.API.Models;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public sealed class EmployeeImportProcessor
{
    private const int MaxPersistedErrors = 1000;

    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;
    private readonly EmployeeImportUploadSessionService _uploadSessionService;
    private readonly ILogger<EmployeeImportProcessor> _logger;

    public EmployeeImportProcessor(
        IDbContextFactory<JobFlowDbContext> dbContextFactory,
        EmployeeImportUploadSessionService uploadSessionService,
        ILogger<EmployeeImportProcessor> logger)
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
        var importJob = await dbContext.Set<EmployeeImportJob>()
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

            importJob.TotalRows = session.Rows.Count;
            await dbContext.SaveChangesAsync();

            // Resolve the first role in the organization to use as default
            var defaultRole = await dbContext.Set<EmployeeRole>()
                .AsNoTracking()
                .Where(r => r.OrganizationId == organizationId)
                .OrderBy(r => r.Name)
                .FirstOrDefaultAsync();

            if (defaultRole is null)
            {
                await MarkFailedAsync(dbContext, importJob, "No employee roles exist for your organization. Please create at least one role before importing.");
                return;
            }

            var employees = dbContext.Set<Employee>();
            var errors = dbContext.Set<EmployeeImportJobError>();

            var emailSourceColumn = columnMappings
                .FirstOrDefault(x => string.Equals(x.Value, EmployeeImportTargetFields.Email, StringComparison.OrdinalIgnoreCase))
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

            var existingByEmail = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
            if (sessionEmails.Count > 0)
            {
                var existing = await employees
                    .Where(e => e.OrganizationId == organizationId
                                && e.Email != null
                                && sessionEmails.Contains(e.Email.ToLower()))
                    .ToListAsync();

                foreach (var row in existing)
                {
                    if (!string.IsNullOrWhiteSpace(row.Email))
                    {
                        existingByEmail[row.Email.Trim().ToLowerInvariant()] = row;
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
                        && string.IsNullOrWhiteSpace(mapped.Email)
                        && string.IsNullOrWhiteSpace(mapped.PhoneNumber))
                    {
                        failedRows++;
                        if (persistedErrors < MaxPersistedErrors)
                        {
                            errors.Add(new EmployeeImportJobError
                            {
                                Id = Guid.NewGuid(),
                                EmployeeImportJobId = jobId,
                                RowNumber = rowNumber,
                                Message = "Row has no usable employee data.",
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            });
                            persistedErrors++;
                        }
                        continue;
                    }

                    // Require at least a first name
                    if (string.IsNullOrWhiteSpace(mapped.FirstName))
                    {
                        failedRows++;
                        if (persistedErrors < MaxPersistedErrors)
                        {
                            errors.Add(new EmployeeImportJobError
                            {
                                Id = Guid.NewGuid(),
                                EmployeeImportJobId = jobId,
                                RowNumber = rowNumber,
                                Message = "First name is required for each employee.",
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            });
                            persistedErrors++;
                        }
                        continue;
                    }

                    Employee entity;
                    if (!string.IsNullOrWhiteSpace(mapped.Email)
                        && existingByEmail.TryGetValue(mapped.Email.Trim().ToLowerInvariant(), out var existingEmployee))
                    {
                        entity = existingEmployee;
                    }
                    else
                    {
                        entity = new Employee
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = organizationId,
                            FirstName = mapped.FirstName!.Trim(),
                            LastName = mapped.LastName?.Trim() ?? string.Empty,
                            RoleId = defaultRole.Id,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await employees.AddAsync(entity);
                    }

                    Merge(entity, mapped);

                    if (!string.IsNullOrWhiteSpace(entity.Email))
                    {
                        existingByEmail[entity.Email.Trim().ToLowerInvariant()] = entity;
                    }

                    succeededRows++;
                }
                catch (Exception ex)
                {
                    failedRows++;
                    if (persistedErrors < MaxPersistedErrors)
                    {
                        errors.Add(new EmployeeImportJobError
                        {
                            Id = Guid.NewGuid(),
                            EmployeeImportJobId = jobId,
                            RowNumber = rowNumber,
                            Message = Truncate(ex.Message, 2000),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        });
                        persistedErrors++;
                    }

                    _logger.LogWarning(ex, "Employee import row failed. JobId={JobId}, Row={RowNumber}", jobId, rowNumber);
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
            _logger.LogError(ex, "Employee import job failed. JobId={JobId}", jobId);
            await MarkFailedAsync(dbContext, importJob, "Import failed unexpectedly. Please retry or contact support.");
        }
    }

    private static async Task MarkFailedAsync(JobFlowDbContext dbContext, EmployeeImportJob importJob, string message)
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

    private static void Merge(Employee entity, MappedEmployeeRow mapped)
    {
        entity.FirstName = Prefer(mapped.FirstName, entity.FirstName) ?? entity.FirstName;
        entity.LastName = Prefer(mapped.LastName, entity.LastName) ?? entity.LastName;
        entity.Email = Prefer(mapped.Email, entity.Email);
        entity.PhoneNumber = Prefer(mapped.PhoneNumber, entity.PhoneNumber);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = true;
        entity.DeactivatedAtUtc = null;
    }

    private static string? Prefer(string? incoming, string? current)
    {
        return string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();
    }

    private static MappedEmployeeRow MapRow(
        Dictionary<string, string?> row,
        Dictionary<string, string?> columnMappings)
    {
        var mapped = new MappedEmployeeRow();

        foreach (var (sourceColumn, targetField) in columnMappings)
        {
            if (string.IsNullOrWhiteSpace(targetField)
                || string.Equals(targetField, EmployeeImportTargetFields.Ignore, StringComparison.OrdinalIgnoreCase))
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
                case EmployeeImportTargetFields.FirstName:
                    mapped.FirstName = value;
                    break;
                case EmployeeImportTargetFields.LastName:
                    mapped.LastName = value;
                    break;
                case EmployeeImportTargetFields.FullName:
                    ApplyFullName(mapped, value);
                    break;
                case EmployeeImportTargetFields.Email:
                    mapped.Email = value;
                    break;
                case EmployeeImportTargetFields.PhoneNumber:
                    mapped.PhoneNumber = value;
                    break;
            }
        }

        return mapped;
    }

    private static void ApplyFullName(MappedEmployeeRow mapped, string fullName)
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

    private sealed class MappedEmployeeRow
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
