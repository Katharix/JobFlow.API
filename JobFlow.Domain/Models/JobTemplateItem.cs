namespace JobFlow.Domain.Models;

public class JobTemplateItem : Entity
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public JobTemplate Template { get; set; } = null!;
}
