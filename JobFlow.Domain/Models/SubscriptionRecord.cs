using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SubscriptionRecord : Entity
{
    public Guid PaymentProfileId { get; set; }
    public string ProviderSubscriptionId { get; set; } = string.Empty;
    public string ProviderPriceId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public PaymentProvider Provider { get; set; }
    public string Status { get; set; } = "active";
    public DateTime StartDate { get; set; }
    public DateTime? CanceledAt { get; set; }

    public virtual CustomerPaymentProfile PaymentProfile { get; set; }
}