namespace JobFlow.Business.Onboarding;

public static class OnboardingPresetKeys
{
    public const string HomeServices = "home_services";
    public const string Construction = "construction";
    public const string CreativeDesign = "creative_design";
    public const string TechRepair = "tech_repair";
    public const string Consulting = "consulting";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
