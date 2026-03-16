using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class PaymentAdjustmentRequestDto
{
    public PaymentProvider Provider { get; set; }
    public string ProviderPaymentId { get; set; } = string.Empty;
    public decimal AdjustmentAmount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Reason { get; set; }
    public string? ProductName { get; set; }
    public Guid? InvoiceId { get; set; }
}
