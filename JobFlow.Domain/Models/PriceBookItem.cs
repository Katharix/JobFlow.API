using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class PriceBookItem : Entity
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PartNumber { get; set; }
        public string? Unit { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? InventoryItemId { get; set; }
        public bool IsTaxable { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal InventoryUnitsPerSale { get; set; } = 1.0m;

        public PriceBookItemType ItemType { get; set; }
        public PriceBookCategory? Category { get; set; }
        public InventoryItem? InventoryItem { get; set; }
    }


}
