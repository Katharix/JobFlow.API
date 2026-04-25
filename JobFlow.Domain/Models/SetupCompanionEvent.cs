namespace JobFlow.Domain.Models;

public class SetupCompanionEvent : Entity
{
    public Guid OrganizationId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string QuestionKey { get; set; } = string.Empty;
    public string? AnswerKey { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
