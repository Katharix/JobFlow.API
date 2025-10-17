using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class PriceBookCategory : Entity
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<PriceBookItem> Items { get; set; } = new List<PriceBookItem>();
    }

}
