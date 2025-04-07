using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    namespace JobFlow.Domain.Models
    {
        public class SubscriptionRecord
        {
            public Guid Id { get; set; }
            public Guid PaymentProfileId { get; set; }
            public string ProviderSubscriptionId { get; set; } = string.Empty;
            public string ProviderPriceId { get; set; } = string.Empty;
            public PaymentProvider Provider { get; set; }
            public string Status { get; set; } = "active";
            public DateTime StartDate { get; set; }
            public DateTime? CanceledAt { get; set; }

            public virtual CustomerPaymentProfile PaymentProfile { get; set; }
        }
    }

}
