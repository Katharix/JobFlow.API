using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class ChangelogEntry : Entity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public ChangelogCategory Category { get; set; } = ChangelogCategory.Feature;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
