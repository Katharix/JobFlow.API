using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public Guid OrganizationClientId { get; set; }
        public Guid? OrderId { get; set; } // Can be null if manually invoiced
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public string Status { get; set; } // Paid, Unpaid, Overdue
        public string? StripeInvoiceId { get; set; }

        public virtual OrganizationClient OrganizationClient { get; set; }
        public virtual Order Order { get; set; }
    }
}
