using Microsoft.AspNetCore.Identity;

namespace JobFlow.Domain.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }                                              
        public Guid OrganizationId { get; set; }
        public string? FirebaseUid { get; set; }
        public Guid? ClientId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Organization Organization { get; set; }
        public OrganizationClient Client { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    }
}
