using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class Assignment : Entity
{
    public Guid JobId { get; set; }
    public virtual Job Job { get; set; }

    // Window vs exact is semantic; both use ScheduledStart/End
    public ScheduleType ScheduleType { get; set; } = ScheduleType.Window;

    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }

    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }
    
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Scheduled;

    // Optional: assignment-level override location (job location can differ from client address)
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<AssignmentAssignee> AssignmentAssignees { get; set; } = new List<AssignmentAssignee>();

    public virtual ICollection<AssignmentOrder> AssignmentOrders { get; set; } = new List<AssignmentOrder>();
}