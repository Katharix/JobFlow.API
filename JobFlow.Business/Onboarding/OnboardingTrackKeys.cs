namespace JobFlow.Business.Onboarding;

public static class OnboardingTrackKeys
{
    public const string GetPaidFast = "get_paid_fast";
    public const string GetOrganizedFirst = "get_organized_first";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
