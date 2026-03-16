namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentOperationResult
{
    public bool Success { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? Message { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}
