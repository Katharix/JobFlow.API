using Microsoft.AspNetCore.Identity;

namespace JobFlow.Domain.Models
{
    public class User : IdentityUser<Guid>
    {
        public Guid OrganizationId { get; set; }
        public string? FirebaseUid { get; set; }
        public Guid? ClientId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Organization Organization { get; set; }
        public OrganizationClient Client { get; set; }
    }
}
