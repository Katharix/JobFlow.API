using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways.Stripe.StripeModels
{
    public class StripeError
    {
        public string? Code { get; set; }
        public string? DeclineCode { get; set; }
        public string? Message { get; set; }
        public string? Param { get; set; }
        public StripeErrorEnum Type { get; set; }

    }
    public enum StripeErrorEnum
    {
        ApiError,
        CardError,
        IdempotencyError,
        InvalidRequestError
    }
}
