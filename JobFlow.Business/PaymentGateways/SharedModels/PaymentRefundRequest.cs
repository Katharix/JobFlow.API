namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentRefundRequest
{
    public Guid? InvoiceId { get; set; }
    public string ProviderPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Reason { get; set; }
    public string? ConnectedAccountId { get; set; }
}
