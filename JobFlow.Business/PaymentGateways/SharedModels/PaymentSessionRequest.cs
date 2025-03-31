using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.PaymentGateways.SharedModels
{
    public class PaymentSessionRequest
    {
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public decimal ApplicationFeeAmount { get; set; }
        public string? ConnectedAccountId { get; set; }
        public string SuccessUrl { get; set; }
    }

}
