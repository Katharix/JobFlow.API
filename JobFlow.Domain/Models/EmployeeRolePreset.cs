namespace JobFlow.Domain.Models;

public class EmployeeRolePreset : Entity
{
    public Guid? OrganizationId { get; set; }
    public string? IndustryKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<EmployeeRolePresetItem> Items { get; set; } = new List<EmployeeRolePresetItem>();
}
