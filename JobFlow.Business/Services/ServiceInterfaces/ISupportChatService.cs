using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface ISupportChatService
{
    Task<Result<SupportChatJoinQueueResponse>> JoinQueueAsync(string customerName, string customerEmail, Guid? customerId = null);
    Task<Result<SupportChatSessionDto>> PickNextCustomerAsync(Guid repId, string repName);
    Task<Result<SupportChatSessionDto>> PickCustomerAsync(Guid sessionId, Guid repId, string repName);
    Task<Result<SupportChatMessageDto>> SendMessageAsync(SupportChatSendMessageRequest request);
    Task<Result<List<SupportChatQueueItemDto>>> GetQueueAsync();
    Task<Result<List<SupportChatMessageDto>>> GetSessionMessagesAsync(Guid sessionId);
    Task<Result> CloseSessionAsync(Guid sessionId);
    Task<Result<SupportChatFileUploadResponse>> UploadFileAsync(Stream stream, string fileName, string contentType);
    Task<Result<SupportChatValidateCustomerResponse>> ValidateCustomerAsync(string email);
    Task<Result<SupportChatSessionDto>> GetSessionAsync(Guid sessionId);
    Task<Result> RemoveFromQueueAsync(Guid sessionId);
}
