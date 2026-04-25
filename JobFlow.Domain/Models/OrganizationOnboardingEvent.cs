namespace JobFlow.Domain.Models;

public class OrganizationOnboardingEvent : Entity
{
    public Guid OrganizationId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;

    // Navigation
    public Organization Organization { get; set; } = null!;
}
