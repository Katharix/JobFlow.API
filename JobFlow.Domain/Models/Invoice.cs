using JobFlow.Domain.Enums;
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
        public Guid? OrderId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public InvoiceStatus Status { get; set; }
        public string? StripeInvoiceId { get; set; }

        public virtual OrganizationClient OrganizationClient { get; set; }
        public virtual Order Order { get; set; }
        public virtual ICollection<PaymentHistory> Payments { get; set; }
        public virtual ICollection<InvoiceLineItem> LineItems { get; set; }
            = new List<InvoiceLineItem>();
    }
}
