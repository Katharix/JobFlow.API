namespace JobFlow.Domain.Models;

/// <summary>
/// Join entity giving an Employee multiple EmployeeRoles.
/// The legacy <see cref="Employee.RoleId"/> is preserved for back-compat;
/// it points at the primary/first role assigned to the employee.
/// </summary>
public class EmployeeRoleAssignment
{
    public Guid EmployeeId { get; set; }
    public Guid EmployeeRoleId { get; set; }

    public Employee Employee { get; set; } = null!;
    public EmployeeRole Role { get; set; } = null!;
}
