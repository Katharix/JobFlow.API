using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SupportHubSession : Entity
{
    public Guid OrganizationId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public SupportHubSessionStatus Status { get; set; } = SupportHubSessionStatus.Queued;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public Organization? Organization { get; set; }
}
