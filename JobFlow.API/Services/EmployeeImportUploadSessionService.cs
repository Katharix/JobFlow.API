using System.Text.Json;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public sealed class EmployeeImportUploadSessionService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDbContextFactory<JobFlowDbContext> _dbContextFactory;

    public EmployeeImportUploadSessionService(IDbContextFactory<JobFlowDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid> SaveAsync(
        Guid organizationId,
        string sourceSystem,
        IReadOnlyList<Dictionary<string, string?>> rows,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var expired = await dbContext.Set<EmployeeImportUploadSession>()
            .Where(x => x.ExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            dbContext.Set<EmployeeImportUploadSession>().RemoveRange(expired);
        }

        var sessionId = Guid.NewGuid();
        var session = new EmployeeImportUploadSession
        {
            Id = sessionId,
            OrganizationId = organizationId,
            SourceSystem = sourceSystem,
            Status = "active",
            TotalRows = rows.Count,
            CreatedAt = now,
            ExpiresAtUtc = now.Add(SessionTtl),
            IsActive = true
        };

        dbContext.Set<EmployeeImportUploadSession>().Add(session);

        var rowEntities = rows.Select((row, index) => new EmployeeImportUploadRow
        {
            Id = Guid.NewGuid(),
            EmployeeImportUploadSessionId = sessionId,
            RowNumber = index + 2,
            RowDataJson = JsonSerializer.Serialize(row, JsonOptions),
            CreatedAt = now,
            IsActive = true
        }).ToList();

        dbContext.Set<EmployeeImportUploadRow>().AddRange(rowEntities);
        await dbContext.SaveChangesAsync(cancellationToken);

        return sessionId;
    }

    public async Task<EmployeeImportUploadData?> GetActiveSessionAsync(Guid sessionId, Guid organizationId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var session = await dbContext.Set<EmployeeImportUploadSession>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.OrganizationId == organizationId, cancellationToken);

        if (session is null || session.ExpiresAtUtc < now || !string.Equals(session.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rows = await dbContext.Set<EmployeeImportUploadRow>()
            .AsNoTracking()
            .Where(x => x.EmployeeImportUploadSessionId == sessionId)
            .OrderBy(x => x.RowNumber)
            .Select(x => new EmployeeImportUploadRowData(
                x.RowNumber,
                JsonSerializer.Deserialize<Dictionary<string, string?>>(x.RowDataJson, JsonOptions)
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)))
            .ToListAsync(cancellationToken);

        return new EmployeeImportUploadData(
            session.Id,
            session.OrganizationId,
            session.SourceSystem,
            session.TotalRows,
            rows);
    }

    public async Task MarkConsumedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var session = await dbContext.Set<EmployeeImportUploadSession>()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Status = "consumed";
        session.ConsumedAtUtc = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed record EmployeeImportUploadData(
    Guid SessionId,
    Guid OrganizationId,
    string SourceSystem,
    int TotalRows,
    IReadOnlyList<EmployeeImportUploadRowData> Rows);

public sealed record EmployeeImportUploadRowData(
    int RowNumber,
    Dictionary<string, string?> Row);
