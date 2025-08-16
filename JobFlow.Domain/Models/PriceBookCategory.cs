using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class PriceBookCategory
    {
        public int Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PriceBookItem> Items { get; set; } = new List<PriceBookItem>();
    }

}
