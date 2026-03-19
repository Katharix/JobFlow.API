namespace JobFlow.API.Models;

public record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    string? AttachmentUrl,
    DateTime SentAt,
    string? SenderName,
    string? SenderAvatarUrl,
    bool IsMine);

public record ChatConversationDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string? Role,
    string Status,
    int UnreadCount,
    ChatMessageDto? LastMessage);

public record CreateConversationRequest(List<string> ParticipantIds);

public record CreateMessageRequest(Guid ConversationId, string Content, string? AttachmentUrl);
