namespace JobFlow.API.Models
{
    public class OnboardingStepDto
    {
        public Guid Id { get; set; }
        public string StepName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
