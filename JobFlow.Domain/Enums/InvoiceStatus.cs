using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Enums
{
    public enum InvoiceStatus 
    { 
        None,
        Paid, 
        Unpaid, 
        Overdue 
    }
}
