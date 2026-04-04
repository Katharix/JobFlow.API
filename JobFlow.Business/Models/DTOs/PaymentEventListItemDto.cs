using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class PaymentEventListItemDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public PaymentProvider PaymentProvider { get; set; }
    public long AmountPaid { get; set; }
    public string Currency { get; set; } = "usd";
    public DateTime PaidAt { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
}