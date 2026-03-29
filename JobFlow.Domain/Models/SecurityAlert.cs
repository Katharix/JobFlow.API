namespace JobFlow.Domain.Models;

public class SecurityAlert : Entity
{
    public string RuleKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EvidenceCount { get; set; }
    public DateTime WindowStartUtc { get; set; }
    public DateTime WindowEndUtc { get; set; }
    public string Status { get; set; } = "Open";
    public string? DetailsJson { get; set; }
}
