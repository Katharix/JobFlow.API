using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class SetDefaultPaymentMethodRequest
    {
        public Guid ProfileId { get; set; }
        public string PaymentMethodId { get; set; } = string.Empty;
    }

}
