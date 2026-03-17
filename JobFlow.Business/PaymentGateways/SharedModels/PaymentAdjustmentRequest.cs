namespace JobFlow.Business.PaymentGateways.SharedModels;

public class PaymentAdjustmentRequest
{
    public string ProviderPaymentId { get; set; } = string.Empty;
    public decimal AdjustmentAmount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Reason { get; set; }
    public string? ConnectedAccountId { get; set; }
    public string? ProductName { get; set; }
    public Guid? InvoiceId { get; set; }
}
