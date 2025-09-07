using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class CustomerPaymentProfile : Entity
    {
        public Guid OwnerId { get; set; }
        public PaymentEntityType OwnerType { get; set; }
        public PaymentProvider Provider { get; set; }
        public string ProviderCustomerId { get; set; } = string.Empty;
        public string? DefaultPaymentMethodId { get; set; }
        public bool IsDelinquent { get; set; } = false;
    }

}
