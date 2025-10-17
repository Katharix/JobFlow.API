using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class InvoiceLineItem : Entity
    {
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
        public virtual Invoice Invoice { get; set; } = null!;
    }
}
