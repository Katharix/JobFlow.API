namespace JobFlow.Domain.Models;

public class OrganizationWorkflowStatus : Entity
{
    public Guid OrganizationId { get; set; }

    public string Category { get; set; } = "JobLifecycle";
    public string StatusKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
