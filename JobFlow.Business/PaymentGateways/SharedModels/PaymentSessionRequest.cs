using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways.SharedModels
{
    public class PaymentSessionRequest
    {
        public string? Mode { get; set; } // "subscription" or "payment"

        // Shared
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
        public string? Email { get; set; }
        public Guid? OrgId { get; set; }

        // Subscription-specific
        public string? StripePriceId { get; set; }
        public string? StripeCustomerId { get; set; }
        public Guid? PaymentProfileId { get; set; }

        // Payment-specific
        public string? ProductName { get; set; }
        public decimal? Amount { get; set; }
        public int? Quantity { get; set; }
        public decimal? ApplicationFeeAmount { get; set; }
        public string? ConnectedAccountId { get; set; }
    }

}
