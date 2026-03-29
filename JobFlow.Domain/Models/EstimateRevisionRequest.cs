using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class EstimateRevisionRequest : Entity
{
    public Guid EstimateId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }
    public int RevisionNumber { get; set; }
    public EstimateRevisionStatus Status { get; set; } = EstimateRevisionStatus.Requested;
    public string RequestMessage { get; set; } = string.Empty;
    public string? OrganizationResponseMessage { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public Estimate Estimate { get; set; } = null!;
    public OrganizationClient OrganizationClient { get; set; } = null!;
    public ICollection<EstimateRevisionAttachment> Attachments { get; set; } = new List<EstimateRevisionAttachment>();
}
