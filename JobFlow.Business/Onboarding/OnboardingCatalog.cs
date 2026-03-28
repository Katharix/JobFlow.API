using JobFlow.Domain.Models;

namespace JobFlow.Business.Onboarding;

public record OnboardingStepDefinition(
    string Key,
    string Title,
    int Order,
    Func<Organization, bool> IsApplicable
);

public static class OnboardingCatalog
{
    private static readonly Dictionary<string, int> PaidFastOrder = new()
    {
        { OnboardingStepKeys.ChooseTrack, 5 },
        { OnboardingStepKeys.ChooseIndustryPreset, 8 },
        { OnboardingStepKeys.ConnectStripe, 10 },
        { OnboardingStepKeys.CreateCustomer, 20 },
        { OnboardingStepKeys.CreateJob, 30 },
        { OnboardingStepKeys.ScheduleJob, 40 },
        { OnboardingStepKeys.CreateInvoice, 50 },
        { OnboardingStepKeys.SendInvoice, 60 },
        { OnboardingStepKeys.ReceivePayment, 70 }
    };

    private static readonly Dictionary<string, int> OrganizedFirstOrder = new()
    {
        { OnboardingStepKeys.ChooseTrack, 5 },
        { OnboardingStepKeys.ChooseIndustryPreset, 8 },
        { OnboardingStepKeys.CreateCustomer, 10 },
        { OnboardingStepKeys.CreateJob, 20 },
        { OnboardingStepKeys.ScheduleJob, 30 },
        { OnboardingStepKeys.CreateInvoice, 40 },
        { OnboardingStepKeys.SendInvoice, 50 },
        { OnboardingStepKeys.ConnectStripe, 60 },
        { OnboardingStepKeys.ReceivePayment, 70 }
    };

    public static readonly IReadOnlyList<OnboardingStepDefinition> Steps =
    [
        new(OnboardingStepKeys.ChooseTrack, "Choose your onboarding path", 5, _ => true),
        new(OnboardingStepKeys.ChooseIndustryPreset, "Select your industry quick-start", 8, _ => true),
        new(OnboardingStepKeys.CreateCustomer, "Create your first customer", 10, _ => true),
        new(OnboardingStepKeys.CreateJob, "Create your first job", 20, _ => true),
        new(OnboardingStepKeys.ScheduleJob, "Schedule the job", 30, _ => true),
        new(OnboardingStepKeys.CreateInvoice, "Create an invoice", 40, _ => true),
        new(OnboardingStepKeys.ConnectStripe, "Connect your payment account", 50, _ => true),
        new(OnboardingStepKeys.SendInvoice, "Send the invoice", 60, _ => true),
        new(
            OnboardingStepKeys.ReceivePayment,
            "Get paid",
            70,
            org => org.IsStripeConnected
        )
    ];

    public static bool IsKnown(string key)
    {
        return Steps.Any(s => s.Key == key);
    }

    public static IEnumerable<OnboardingStepDefinition> ApplicableSteps(Organization org)
    {
        var orderMap = GetOrderMap(org.OnboardingTrack);
        return Steps
            .Where(s => s.IsApplicable(org))
            .Select(step =>
            {
                var order = orderMap.TryGetValue(step.Key, out var mapped)
                    ? mapped
                    : step.Order;

                return step with { Order = order };
            })
            .OrderBy(s => s.Order);
    }

    private static Dictionary<string, int> GetOrderMap(string? trackKey)
    {
        var normalized = OnboardingTrackKeys.Normalize(trackKey);
        return normalized == OnboardingTrackKeys.GetOrganizedFirst
            ? OrganizedFirstOrder
            : PaidFastOrder;
    }
}