using JobFlow.API.Models;

namespace JobFlow.API.Validators;

public sealed class OnboardingDtoValidator : SafeRequestValidator<OnboardingDto>
{
    public OnboardingDtoValidator() : base("OrganizationName") { }
}

public sealed class MarkStepRequestDtoValidator : SafeRequestValidator<MarkStepRequestDto>
{
    public MarkStepRequestDtoValidator() : base("StepKey") { }
}

public sealed class OnboardingStepDtoApiValidator : SafeRequestValidator<OnboardingStepDto>
{
    public OnboardingStepDtoApiValidator() : base("StepKey") { }
}
