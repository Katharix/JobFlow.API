using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class PriceBookItem
    {
        public int Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Unit { get; set; } = null!;
        public decimal PricePerUnit { get; set; }
        public string? Category { get; set; }
        public bool IsTaxable { get; set; }

        public int? InventoryItemId { get; set; }
        public InventoryItem? InventoryItem { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
