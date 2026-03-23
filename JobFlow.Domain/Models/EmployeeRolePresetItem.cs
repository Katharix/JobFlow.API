namespace JobFlow.Domain.Models;

public class EmployeeRolePresetItem : Entity
{
    public Guid PresetId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public EmployeeRolePreset Preset { get; set; }
}
