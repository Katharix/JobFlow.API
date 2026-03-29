using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SupportHubTicket : Entity
{
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public SupportHubTicketStatus Status { get; set; } = SupportHubTicketStatus.Normal;
    public DateTimeOffset? LastActivityAt { get; set; }

    public Organization? Organization { get; set; }
}
