namespace JobFlow.Business.ModelErrors
{
    public static class OnboardingErrors
    {
        public static Error UnknownStep(string stepKey) =>
            Error.Failure(
                "Onboarding",
                $"Unknown onboarding step: {stepKey}"
            );

        public static Error OrganizationNotFound =>
            Error.NotFound(
                "Organization",
                "Organization not found for onboarding."
            );
    }
}