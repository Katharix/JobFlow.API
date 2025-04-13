using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class JobOrder
    {
        public Guid JobId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Job Job { get; set; }
        public virtual Order Order { get; set; }
    }

}
