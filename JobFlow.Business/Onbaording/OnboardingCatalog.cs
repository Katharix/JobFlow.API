using JobFlow.Business.Onbaording;
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
    public static readonly IReadOnlyList<OnboardingStepDefinition> Steps =
    [
        new(OnboardingStepKeys.CreateCustomer, "Create your first customer", 10, _ => true),
        new(OnboardingStepKeys.CreateJob, "Create your first job", 20, _ => true),
        new(OnboardingStepKeys.ScheduleJob, "Schedule the job", 30, _ => true),
        new(OnboardingStepKeys.CreateInvoice, "Create an invoice", 40, _ => true),
        new(OnboardingStepKeys.SendInvoice, "Send the invoice", 50, _ => true),
        new(
            OnboardingStepKeys.ReceivePayment,
            "Get paid",
            60,
            org => !string.IsNullOrEmpty(org.StripeConnectAccountId)
        )
    ];

    public static bool IsKnown(string key) =>
        Steps.Any(s => s.Key == key);

    public static IEnumerable<OnboardingStepDefinition> ApplicableSteps(Organization org) =>
        Steps.Where(s => s.IsApplicable(org)).OrderBy(s => s.Order);
}