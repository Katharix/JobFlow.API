namespace JobFlow.Domain.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public virtual User User { get; set; }
    public virtual SystemRole Role { get; set; }
}