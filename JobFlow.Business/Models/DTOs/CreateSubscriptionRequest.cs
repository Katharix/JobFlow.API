using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class CreateSubscriptionRequest
    {
        public Guid PaymentProfileId { get; set; }
        public string ProviderSubscriptionId { get; set; }
        public string ProviderPriceId { get; set; }
        public string? Status { get; set; } // Optional, default = "active"
    }

}
