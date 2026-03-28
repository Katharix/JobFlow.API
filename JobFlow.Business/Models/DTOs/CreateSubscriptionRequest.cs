namespace JobFlow.Business.Models.DTOs;

public class CreateSubscriptionRequest
{
    public Guid PaymentProfileId { get; set; }
    public string ProviderSubscriptionId { get; set; } = string.Empty;
    public string ProviderPriceId { get; set; } = string.Empty;
    public string? Status { get; set; } // Optional, default = "active"
}