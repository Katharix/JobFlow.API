using JobFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class PriceBookItemDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? PartNumber { get; set; }
        public string? Unit { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public PriceBookItemType ItemType { get; set; }
        public decimal InventoryUnitsPerSale { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Category { get; set; }
    }
}
