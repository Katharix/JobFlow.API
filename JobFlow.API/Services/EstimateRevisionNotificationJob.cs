using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Services;

public interface IEstimateRevisionNotificationJob
{
    Task SendRevisionRequestedNotificationAsync(
        Guid organizationId,
        Guid organizationClientId,
        Guid estimateId,
        string revisionMessage);
}

public sealed class EstimateRevisionNotificationJob : IEstimateRevisionNotificationJob
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EstimateRevisionNotificationJob> _logger;

    public EstimateRevisionNotificationJob(
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<EstimateRevisionNotificationJob> logger)
    {
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendRevisionRequestedNotificationAsync(
        Guid organizationId,
        Guid organizationClientId,
        Guid estimateId,
        string revisionMessage)
    {
        try
        {
            var organization = await _unitOfWork.RepositoryOf<Organization>().Query()
                .FirstOrDefaultAsync(x => x.Id == organizationId);

            var client = await _unitOfWork.RepositoryOf<OrganizationClient>().Query()
                .FirstOrDefaultAsync(x => x.Id == organizationClientId && x.OrganizationId == organizationId);

            var estimate = await _unitOfWork.RepositoryOf<Estimate>().Query()
                .FirstOrDefaultAsync(x => x.Id == estimateId);

            if (organization is null || client is null || estimate is null)
            {
                _logger.LogWarning(
                    "Skipping estimate revision notification — missing data. Org={OrgId}, Client={ClientId}, Estimate={EstimateId}",
                    organizationId, organizationClientId, estimateId);
                return;
            }

            await _notificationService.SendOrganizationEstimateRevisionRequestedNotificationAsync(
                organization, client, estimate, revisionMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send estimate revision notification for Estimate={EstimateId}",
                estimateId);
        }
    }
}
