namespace JobFlow.Domain.Models;

public class User : Entity
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PreferredLanguage { get; set; }
    public Guid OrganizationId { get; set; }
    public string? FirebaseUid { get; set; }
    public Guid? ClientId { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationClient Client { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}