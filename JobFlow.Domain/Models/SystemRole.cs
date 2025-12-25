namespace JobFlow.Domain.Models;

public class SystemRole
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}