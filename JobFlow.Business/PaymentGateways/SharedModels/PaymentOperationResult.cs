namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentOperationResult
{
    public bool Success { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? Message { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public string? ProviderPriceId { get; set; }
    public DateTime? SubscriptionExpiresAtUtc { get; set; }
}
