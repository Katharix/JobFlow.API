using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Organization
    {
        public Guid Id { get; set; }
        public Guid OrganizationTypeId { get; set; }
        public Guid? StripeCustomerId { get; set; }
        public string? StripeConnectedAccountId { get; set; }
        public string? ZipCode { get; set; }
        public string? OrganizationName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
        public bool HasFreeAccount { get; set; }
        public PaymentProvider PaymentProvider { get; set; } = PaymentProvider.Stripe;


        public virtual StripeCustomer? StripeCustomer { get; set; }
        public virtual OrganizationType? OrganizationType { get; set; }
    }
}
