using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class JobUpdate : Entity
{
    public Guid JobId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }
    public JobUpdateType Type { get; set; }
    public string? Message { get; set; }
    public JobLifecycleStatus? Status { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public Job Job { get; set; } = null!;
    public ICollection<JobUpdateAttachment> Attachments { get; set; } = new List<JobUpdateAttachment>();
}
