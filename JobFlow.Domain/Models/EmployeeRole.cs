namespace JobFlow.Domain.Models;

public class EmployeeRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}