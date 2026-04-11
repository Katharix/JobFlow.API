using JobFlow.Business;
using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Security;

[ScopedService]
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IUnitOfWork unitOfWork, ILogger<AuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.RepositoryOf<AuditLog>();
            await repository.AddAsync(new AuditLog
            {
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                Category = request.Category,
                Action = request.Action,
                ResourceType = request.ResourceType,
                ResourceId = request.ResourceId,
                Path = request.Path,
                Method = request.Method,
                StatusCode = request.StatusCode,
                Success = request.Success,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                DetailsJson = request.DetailsJson,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.UserId
            });

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit log entry for {Method} {Path}", request.Method, request.Path);
        }
    }

    public async Task<Result<CursorPagedResponseDto<AuditLogDto>>> GetAuditLogsAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? category,
        bool? success,
        int pageSize = 100,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.RepositoryOf<AuditLog>();
        var query = repo.QueryWithNoTracking();

        if (fromUtc.HasValue)
            query = query.Where(a => a.CreatedAt >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.CreatedAt <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        if (success.HasValue)
            query = query.Where(a => a.Success == success.Value);

        if (!string.IsNullOrWhiteSpace(cursor) && DateTime.TryParse(cursor, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursorDate))
            query = query.Where(a => a.CreatedAt < cursorDate);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(pageSize + 1)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                OrganizationId = a.OrganizationId,
                UserId = a.UserId,
                Category = a.Category,
                Action = a.Action,
                ResourceType = a.ResourceType,
                ResourceId = a.ResourceId,
                Path = a.Path,
                Method = a.Method,
                StatusCode = a.StatusCode,
                Success = a.Success,
                IpAddress = a.IpAddress,
                DetailsJson = a.DetailsJson,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            nextCursor = items[pageSize].CreatedAt.ToString("O");
            items = items.Take(pageSize).ToList();
        }

        return Result.Success(new CursorPagedResponseDto<AuditLogDto>
        {
            Items = items,
            NextCursor = nextCursor
        });
    }
}
