using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOnboardingService
{
    Task<Result<IEnumerable<OnboardingStepDto>>> GetChecklistAsync(Guid organizationId);
    Task<Result> MarkStepCompleteAsync(Guid organizationId, string stepKey);
    Task<Result<bool>> MarkOrganizationCompleteIfEligibleAsync(Guid organizationId);
    Task<Result<OnboardingQuickStartStateDto>> GetQuickStartStateAsync(Guid organizationId);
    Task<Result<OnboardingQuickStartStateDto>> ApplyQuickStartAsync(
        Guid organizationId,
        OnboardingQuickStartApplyRequestDto request);
    Task<Result> RecordAnalyticsEventAsync(Guid organizationId, string stepName, string eventType);
    Task<Result<OnboardingIndustryDefaultsDto>> GetIndustryDefaultsAsync(Guid organizationId);
    Task<Result> SeedIndustryDefaultsAsync(Guid organizationId);
    Task<Result> DeferPaymentSetupAsync(Guid organizationId);
}