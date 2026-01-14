namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentSessionRequest
{
    public string? Mode { get; set; }

    // Shared
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? Email { get; set; }
    public Guid? OrgId { get; set; }
    
    public Guid? InvoiceId { get; set; }

    // Subscription-specific
    public string? StripePriceId { get; set; }
    public string? StripeCustomerId { get; set; }
    public Guid? PaymentProfileId { get; set; }


    // Payment-specific
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationClientId { get; set; }
    public string? ProductName { get; set; }
    public decimal? Amount { get; set; }
    public decimal? DepositAmount { get; set; }
    public int? Quantity { get; set; }
    public string? ConnectedAccountId { get; set; }
}