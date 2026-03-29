namespace JobFlow.Domain.Models;

public class OrganizationOnboardingStep : Entity
{
    public Guid OrganizationId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
}