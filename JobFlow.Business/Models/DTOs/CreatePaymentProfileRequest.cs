using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs
{
    public class CreatePaymentProfileRequest
    {
        public PaymentProvider Provider { get; set; }
        public string ProviderCustomerId { get; set; } = string.Empty;
    }
}