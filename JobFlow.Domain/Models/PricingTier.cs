using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class PricingTier
    {
        public Guid Id { get; set; }
        public PricingTierType TierType { get; set; }
        public PricingDurationType DurationType { get; set; }
        public double Price { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
    public enum PricingTierType
{
        None,
        GoPrice,
        FlowPrice,
        MaxPrice
    }
    public enum PricingDurationType
    {
        None,
        Monthly,
        Yearly,
    }
}
