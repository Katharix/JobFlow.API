using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class PaymentRefundRequestDto
{
    public PaymentProvider Provider { get; set; }
    public Guid? InvoiceId { get; set; }
    public string ProviderPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Reason { get; set; }
}
