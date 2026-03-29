using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class FollowUpRun : Entity
{
    public Guid FollowUpSequenceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }
    public Guid TriggerEntityId { get; set; }
    public FollowUpSequenceType SequenceType { get; set; }
    public FollowUpRunStatus Status { get; set; } = FollowUpRunStatus.Scheduled;
    public FollowUpStopReason StopReason { get; set; } = FollowUpStopReason.None;
    public int NextStepOrder { get; set; } = 1;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public FollowUpSequence? Sequence { get; set; }
    public ICollection<FollowUpExecutionLog> ExecutionLogs { get; set; } = new List<FollowUpExecutionLog>();
}
