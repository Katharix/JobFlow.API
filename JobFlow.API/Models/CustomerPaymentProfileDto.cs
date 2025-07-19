using JobFlow.Domain.Enums;

namespace JobFlow.API.Models
{
    public class CustomerPaymentProfileDto
    {
        public Guid OwnerId { get; set; }
        public PaymentEntityType OwnerType { get; set; }
        public PaymentProvider Provider { get; set; }
        public string ProviderCustomerId { get; set; } = string.Empty;
    }
}
