using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class FollowUpStep : Entity
{
    public Guid FollowUpSequenceId { get; set; }
    public int StepOrder { get; set; }
    public int DelayHours { get; set; }
    public FollowUpChannel? ChannelOverride { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public bool IsEscalation { get; set; }

    public FollowUpSequence? Sequence { get; set; }
}
