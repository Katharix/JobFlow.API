using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class OrganizationService
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string ServiceName { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }

        public virtual Organization Organization { get; set; }
    }
}
