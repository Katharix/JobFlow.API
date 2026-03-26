using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class FollowUpSequence : Entity
{
    public Guid OrganizationId { get; set; }
    public FollowUpSequenceType SequenceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool StopOnClientReply { get; set; } = true;
    public FollowUpChannel DefaultChannel { get; set; } = FollowUpChannel.Email;

    public ICollection<FollowUpStep> Steps { get; set; } = new List<FollowUpStep>();
}
