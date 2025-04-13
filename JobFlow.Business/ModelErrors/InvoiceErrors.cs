using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class InvoiceErrors
    {
        public static readonly Error NotFound = Error.NotFound("Invoice.NotFound", "The invoice was not found.");
    }
}
