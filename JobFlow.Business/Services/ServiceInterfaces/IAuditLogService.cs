using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogWriteRequest request, CancellationToken cancellationToken = default);
}
