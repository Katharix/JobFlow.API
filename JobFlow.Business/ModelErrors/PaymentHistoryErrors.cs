using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class PaymentHistoryErrors
    {
        public static readonly Error NotFound = Error.NotFound("PaymentHistory.NotFound", "The payment record was not found.");
    }
}
