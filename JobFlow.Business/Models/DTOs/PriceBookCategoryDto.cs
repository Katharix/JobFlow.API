using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class PriceBookCategoryDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int ItemCount { get; set; }
    }
}
