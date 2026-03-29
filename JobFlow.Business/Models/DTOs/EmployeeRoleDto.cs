namespace JobFlow.Business.Models.DTOs;

public class EmployeeRoleDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }
}