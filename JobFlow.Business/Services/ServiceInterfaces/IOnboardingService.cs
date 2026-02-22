using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOnboardingService
{
    Task<Result<IEnumerable<OnboardingStepDto>>> GetChecklistAsync(Guid organizationId);
    Task<Result> MarkStepCompleteAsync(Guid organizationId, string stepKey);
}