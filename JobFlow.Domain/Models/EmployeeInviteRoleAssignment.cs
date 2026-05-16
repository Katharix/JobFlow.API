namespace JobFlow.Domain.Models;

/// <summary>
/// Join entity carrying the set of EmployeeRoles to grant when an
/// EmployeeInvite is accepted. The legacy <see cref="EmployeeInvite.RoleId"/>
/// is preserved for back-compat; it points at the primary/first role.
/// </summary>
public class EmployeeInviteRoleAssignment
{
    public Guid EmployeeInviteId { get; set; }
    public Guid EmployeeRoleId { get; set; }

    public EmployeeInvite EmployeeInvite { get; set; } = null!;
    public EmployeeRole Role { get; set; } = null!;
}
