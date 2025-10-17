using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class InventoryItem : Entity
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Unit { get; set; } = null!;
        public decimal? CostPerUnit { get; set; }
        public int? QuantityInStock { get; set; }
        public string? Category { get; set; }
    }

}
