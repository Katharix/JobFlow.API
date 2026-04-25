using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;


public class OrganizationClientDto
{
    public Guid? Id { get; set; }
    public Guid? OrganizationId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public bool SmsConsentGiven { get; set; }
    public OrganizationDto? Organization { get; set; }
    public string? FullName => $"{FirstName} {LastName}".Trim();

}

public class OrganizationDto
{
    public Guid? Id { get; set; }
    public string? OrganizationName { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? Email { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public decimal DefaultTaxRate { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? OnBoardingComplete { get; set; }
    public bool CanAcceptPayments { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public string? IndustryKey { get; set; }
    public Guid OrganizationTypeId { get; set; }
    public PaymentProvider PaymentProvider { get; set; }
    public string? StripeConnectAccountId { get; set; }
    public bool IsStripeConnected { get; set; }
    public string? SquareMerchantId { get; set; }
    public bool IsSquareConnected { get; set; }
    public bool PaymentSetupDeferred { get; set; }
    public OrgSize OrgSize { get; set; }
    public DateTimeOffset? FirstRealEstimateSentAt { get; set; }
    public DateTimeOffset? ReferralCtaShownAt { get; set; }
}