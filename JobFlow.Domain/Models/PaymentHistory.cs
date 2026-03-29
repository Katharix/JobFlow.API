using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class PaymentHistory : Entity
{
    public PaymentProvider PaymentProvider { get; set; }
    public PaymentEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public Guid? InvoiceId { get; set; } // Can link to your JobFlow Invoice table
    public string? StripeInvoiceId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
    public long AmountPaid { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RawEventJson { get; set; } = string.Empty;
    public virtual Invoice Invoice { get; set; } = null!;
}