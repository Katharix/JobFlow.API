using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class StripeCustomer
    {
        public Guid Id { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? PaymentMethod { get; set; }
        public bool Delinqent { get; set; }
    }
}
