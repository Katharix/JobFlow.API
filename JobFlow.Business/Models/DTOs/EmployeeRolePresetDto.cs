namespace JobFlow.Business.Models.DTOs;

public class EmployeeRolePresetDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? IndustryKey { get; set; }
    public bool IsSystem { get; set; }
    public Guid? OrganizationId { get; set; }
    public List<EmployeeRolePresetItemDto> Items { get; set; } = new();
}
