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
            dto.SubscriptionStatus = latestSubscription?.Status;
            dto.SubscriptionExpiresAt = org.SubscriptionExpiresAt;
        }
        else
        {
            dto.SubscriptionPlanName = org.SubscriptionPlanName;
            dto.SubscriptionStatus = org.SubscriptionStatus;
            dto.SubscriptionExpiresAt = org.SubscriptionExpiresAt;
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

    public async Task<Result<Organization>> GetBySquareMerchantIdAsync(string squareMerchantId)
    {
        var org = await _organizations
            .FirstOrDefaultAsync(o => o.SquareMerchantId == squareMerchantId);

        return org is null
            ? Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound)
            : Result.Success(org);
    }

    public async Task MarkSquareDisconnectedAsync(string squareMerchantId)
    {
        var org = await _organizations
            .FirstOrDefaultAsync(o => o.SquareMerchantId == squareMerchantId);

        if (org == null)
            return;

        org.IsSquareConnected = false;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Result> UpdateSubscriptionStateAsync(Guid organizationId, string? subscriptionStatus, string? subscriptionPlanName = null, DateTime? subscriptionExpiresAt = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure(OrganizationErrors.NullOrEmptyId);

        var organization = _organizations.FirstOrDefault(org => org.Id == organizationId);
        if (organization == null)
            return Result.Failure(OrganizationErrors.OrganizationNotFound);

        if (!string.IsNullOrWhiteSpace(subscriptionStatus))
            organization.SubscriptionStatus = subscriptionStatus.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(subscriptionPlanName))
            organization.SubscriptionPlanName = subscriptionPlanName.Trim();

        if (subscriptionExpiresAt.HasValue)
            organization.SubscriptionExpiresAt = subscriptionExpiresAt.Value.ToUniversalTime();

        _unitOfWork.RepositoryOf<Organization>().Update(organization);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
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

    public async Task<Result<OrganizationDto>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request)
    {
        var organization = _organizations.FirstOrDefault(org => org.Id == organizationId);
        if (organization == null)
            return Result.Failure<OrganizationDto>(OrganizationErrors.OrganizationNotFound);

        if (request.OrganizationName != null)
            organization.OrganizationName = request.OrganizationName.Trim();
        if (request.OrganizationTypeId != null)
            organization.OrganizationTypeId = request.OrganizationTypeId.Value;
        if (request.ContactFirstName != null)
            organization.ContactFirstName = request.ContactFirstName.Trim();
        if (request.ContactLastName != null)
            organization.ContactLastName = request.ContactLastName.Trim();
        if (request.EmailAddress != null)
            organization.EmailAddress = request.EmailAddress.Trim();
        if (request.PhoneNumber != null)
            organization.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Address1 != null)
            organization.Address1 = request.Address1.Trim();
        if (request.Address2 != null)
            organization.Address2 = request.Address2;
        if (request.City != null)
            organization.City = request.City.Trim();
        if (request.State != null)
            organization.State = request.State.Trim();
        if (request.ZipCode != null)
            organization.ZipCode = request.ZipCode.Trim();

        _unitOfWork.RepositoryOf<Organization>().Update(organization);
        await _unitOfWork.SaveChangesAsync();

        return await GetOrganizationDtoById(organizationId);
    }

    public async Task<Result<Organization>> UpdateIndustryAsync(Guid organizationId, string? industryKey)
    {
        var organization = _organizations.FirstOrDefault(org => org.Id == organizationId);
        if (organization == null)
        {
            return Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound);
        }

        organization.IndustryKey = string.IsNullOrWhiteSpace(industryKey) ? null : industryKey.Trim();
        _unitOfWork.RepositoryOf<Organization>().Update(organization);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(organization);
    }
}