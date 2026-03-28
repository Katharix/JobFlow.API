namespace JobFlow.Business.Models.DTOs;

public class EmployeeRolePresetItemDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
