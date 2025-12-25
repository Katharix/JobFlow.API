using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class Organization : Entity
{
    public Guid OrganizationTypeId { get; set; }
    public string? ZipCode { get; set; }
    public string? OrganizationName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public decimal DefaultTaxRate { get; set; } = 0.00m;
    public bool EnableTax { get; set; } = false;
    public bool HasFreeAccount { get; set; }
    public bool OnBoardingComplete { get; set; }
    public string? StripeConnectAccountId { get; set; }

    public bool CanAcceptPayments =>
        PaymentProvider == PaymentProvider.Stripe &&
        !string.IsNullOrWhiteSpace(StripeConnectAccountId);

    public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.Stripe;
    public ICollection<CustomerPaymentProfile> PaymentProfiles { get; set; } = new List<CustomerPaymentProfile>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();

    public ICollection<OrganizationOnboardingStep> OnboardingSteps { get; set; } =
        new List<OrganizationOnboardingStep>();

    public virtual OrganizationType? OrganizationType { get; set; }
}