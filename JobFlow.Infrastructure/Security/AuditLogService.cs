using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
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
                CreatedBy = request.UserId?.ToString()
            });

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit log entry for {Method} {Path}", request.Method, request.Path);
        }
    }
}
