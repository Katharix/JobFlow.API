using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class TrialTrackingService : ITrialTrackingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBrevoService _brevoService;
    private readonly ILogger<TrialTrackingService> _logger;

    public TrialTrackingService(
        IUnitOfWork unitOfWork,
        IBrevoService brevoService,
        ILogger<TrialTrackingService> logger)
    {
        _unitOfWork = unitOfWork;
        _brevoService = brevoService;
        _logger = logger;
    }

    public async Task TrackAsync(Guid organizationId, string eventKey)
    {
        try
        {
            var orgs = _unitOfWork.RepositoryOf<Organization>();
            var email = await orgs.Query()
                .Where(x => x.Id == organizationId)
                .Select(x => x.EmailAddress)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(email)) return;

            var success = await _brevoService.TrackActivationEventAsync(email, eventKey);
            if (!success)
                _logger.LogWarning(
                    "Trial activation tracking returned false for org {OrgId}, event {EventKey}",
                    organizationId, eventKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Trial activation tracking failed for org {OrgId}, event {EventKey}",
                organizationId, eventKey);
        }
    }
}
