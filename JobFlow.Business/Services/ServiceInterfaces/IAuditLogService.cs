using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default);

    Task<Result<CursorPagedResponseDto<AuditLogDto>>> GetAuditLogsAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? category,
        bool? success,
        int pageSize = 100,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}
