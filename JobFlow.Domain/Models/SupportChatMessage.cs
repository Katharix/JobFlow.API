using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class SupportChatMessage : Entity
{
    public Guid SessionId { get; set; }
    public Guid? SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public SupportChatSenderRole SenderRole { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public SupportChatSession Session { get; set; } = null!;
}
