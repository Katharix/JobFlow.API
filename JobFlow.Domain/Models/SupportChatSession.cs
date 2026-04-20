using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SupportChatSession : Entity
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? AssignedRepId { get; set; }
    public string? AssignedRepName { get; set; }
    public SupportChatSessionStatus Status { get; set; } = SupportChatSessionStatus.Queued;
    public DateTime? StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int EstimatedWaitSeconds { get; set; }

    public User? Customer { get; set; }
    public User? AssignedRep { get; set; }
    public ICollection<SupportChatMessage> Messages { get; set; } = new List<SupportChatMessage>();
}
