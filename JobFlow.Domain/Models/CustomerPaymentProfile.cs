using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class CustomerPaymentProfile : Entity
{
    public Guid OwnerId { get; set; }
    public PaymentEntityType OwnerType { get; set; }
    public PaymentProvider Provider { get; set; }
    public string ProviderCustomerId { get; set; } = string.Empty;
    public string? DefaultPaymentMethodId { get; set; }
    public bool IsDelinquent { get; set; } = false;
}