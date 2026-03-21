using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class Job : Entity
{
    public JobLifecycleStatus LifecycleStatus { get; set; }
    public InvoicingWorkflow? InvoicingWorkflow { get; set; }
    public string? Title { get; set; }
    public string? Comments { get; set; }

    public Guid OrganizationClientId { get; set; }
    public virtual OrganizationClient OrganizationClient { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public virtual ICollection<JobTracking> JobTrackings { get; set; } = new List<JobTracking>();
    public virtual ICollection<JobUpdate> JobUpdates { get; set; } = new List<JobUpdate>();
}