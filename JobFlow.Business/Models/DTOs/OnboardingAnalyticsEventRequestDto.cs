namespace JobFlow.Business.Models.DTOs;

public class OnboardingAnalyticsEventRequestDto
{
    public string StepName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
}
