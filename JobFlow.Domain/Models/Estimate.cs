using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class Estimate : Entity
{
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }

    public string EstimateNumber { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;

    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }

    // Public unauthenticated link
    public string PublicToken { get; set; } = string.Empty;
    public DateTimeOffset? PublicTokenExpiresAt { get; set; }

    public OrganizationClient? OrganizationClient { get; set; }
    public ICollection<EstimateLineItem> LineItems { get; set; } = new List<EstimateLineItem>();
}