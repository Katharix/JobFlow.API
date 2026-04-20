using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public record SupportChatJoinQueueRequest(
    string CustomerName,
    string CustomerEmail);

public record SupportChatJoinQueueResponse(
    Guid SessionId,
    int QueuePosition,
    int EstimatedWaitSeconds);

public record SupportChatSessionDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    Guid? CustomerId,
    Guid? AssignedRepId,
    string? AssignedRepName,
    SupportChatSessionStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? ClosedAt,
    int EstimatedWaitSeconds,
    int QueuePosition);

public record SupportChatMessageDto(
    Guid Id,
    Guid SessionId,
    Guid? SenderId,
    string SenderName,
    SupportChatSenderRole SenderRole,
    string Content,
    string? FileUrl,
    string? FileName,
    long? FileSize,
    DateTime SentAt,
    bool IsRead);

public record SupportChatQueueItemDto(
    Guid SessionId,
    string CustomerName,
    string CustomerEmail,
    int QueuePosition,
    int EstimatedWaitSeconds,
    DateTime JoinedAt);

public record SupportChatSendMessageRequest(
    Guid SessionId,
    Guid? SenderId,
    string SenderName,
    SupportChatSenderRole SenderRole,
    string Content,
    string? FileUrl = null,
    string? FileName = null,
    long? FileSize = null);

public record SupportChatFileUploadResponse(
    string FileUrl,
    string FileName,
    long FileSize);

public record SupportChatValidateCustomerResponse(
    bool IsValid,
    Guid? UserId,
    string? DisplayName,
    string? Error);
