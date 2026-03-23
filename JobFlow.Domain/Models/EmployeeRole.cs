namespace JobFlow.Domain.Models;

public class EmployeeRole
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}