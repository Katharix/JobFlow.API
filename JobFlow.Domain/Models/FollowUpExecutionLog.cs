using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class FollowUpExecutionLog : Entity
{
    public Guid FollowUpRunId { get; set; }
    public int StepOrder { get; set; }
    public FollowUpChannel Channel { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool WasSent { get; set; }
    public string? FailureReason { get; set; }

    public FollowUpRun? Run { get; set; }
}
