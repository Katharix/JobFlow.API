using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class OrganizationBranding : Entity
    {
        public Guid OrganizationId { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? BusinessName { get; set; }
        public string? Tagline { get; set; }
        public string? FooterNote { get; set; }
        public virtual Organization Organization { get; set; } = null!;
    }

}
