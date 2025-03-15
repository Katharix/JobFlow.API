using Microsoft.AspNetCore.Identity;

namespace JobFlow.Domain.Models
{
    public class User : IdentityUser<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid? ClientId { get; set; }
        public Organization Organization { get; set; }
        public OrganizationClient Client { get; set; }
    }
}
