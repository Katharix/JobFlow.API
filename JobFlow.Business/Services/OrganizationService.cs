using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationService : IOrganizationService
{
    private readonly IQueryable<Organization> _organizations;
    private readonly IUnitOfWork _unitOfWork;
    private ILogger<OrganizationService> _logger;
    private IOnboardingService _onboardingService;
    private readonly IRepository<SubscriptionRecord> _subscriptions;

    public OrganizationService(
        IUnitOfWork unitOfWork, 
        ILogger<OrganizationService> logger,
        IOnboardingService onboardingService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _onboardingService = onboardingService;
        _subscriptions = unitOfWork.RepositoryOf<SubscriptionRecord>();
        _organizations = _unitOfWork.RepositoryOf<Organization>()
            .Query()
            .Include(e => e.OrganizationType)
            .Include(e => e.PaymentProfiles);
    }

    public async Task<Result> DeleteOrganization(Guid organizationId)
    {
        var organizationToDelete = _organizations.FirstOrDefault(org => org.Id == organizationId);
        if (organizationToDelete == null) return Result.Failure(OrganizationErrors.OrganizationNotFound);
        _unitOfWork.RepositoryOf<Organization>().Remove(organizationToDelete);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success($"Organization: {organizationToDelete.OrganizationName} has been removed successfully.");
    }

    public async Task<Result<IEnumerable<Organization>>> GetAllOrganizations()
    {
        var organizations = _organizations.AsEnumerable();

        if (!organizations.Any())
            return Result.Failure<IEnumerable<Organization>>(OrganizationErrors.OrganizationNotFound);
        return Result.Success(organizations);
    }

    public async Task<Result<Organization>> GetOrganiztionById(Guid orgId)
    {
        var organization = _organizations.FirstOrDefault(org => org.Id == orgId);

        if (organization == null) return Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound);
        return Result.Success(organization);
    }

    public async Task<Result<OrganizationDto>> GetOrganizationDtoById(Guid orgId)
    {
        var orgResult = await GetOrganiztionById(orgId);
        if (orgResult.IsFailure)
            return Result.Failure<OrganizationDto>(orgResult.Error);

        var org = orgResult.Value;
        var dto = org.Adapt<OrganizationDto>();

        var paymentProfileIds = await _unitOfWork.RepositoryOf<CustomerPaymentProfile>()
            .Query()
            .AsNoTracking()
            .Where(p => p.OwnerId == orgId)
            .Select(p => p.Id)
            .ToListAsync();

        if (paymentProfileIds.Count > 0)
        {
            var latestSubscription = await _subscriptions.Query()
                .AsNoTracking()
                .Where(s => paymentProfileIds.Contains(s.PaymentProfileId))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            dto.SubscriptionPlanName = latestSubscription?.PlanName;
        }

        return Result.Success(dto);
    }
    public async Task MarkStripeConnectedAsync(string stripeAccountId)
    {
        var org = await _organizations
           .FirstOrDefaultAsync(o => o.StripeConnectAccountId == stripeAccountId);

        if (org == null)
            return;

        org.IsStripeConnected = true;

        await _unitOfWork.SaveChangesAsync();

        await _onboardingService.MarkStepCompleteAsync(
            org.Id,
            OnboardingStepKeys.ConnectStripe
        );
    }

    public async Task<Result<Organization>> UpsertOrganization(Organization model)
    {
        if (model.Id == Guid.Empty)
        {
            _unitOfWork.RepositoryOf<Organization>().Add(model);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            var organization = _organizations.FirstOrDefault(org => org.Id == model.Id);
            if (organization == null) return Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound);
            _unitOfWork.RepositoryOf<Organization>().Update(model);
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Success(model);
    }
}