using System.Security.Claims;
using System.Text.Json;
using JobFlow.API.Models;
using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Hubs;

public class ChatHub : Hub
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwilioService _twilioService;
    private readonly IHubContext<ClientChatHub> _clientChatHubContext;

    public ChatHub(
        IUserService userService,
        IUnitOfWork unitOfWork,
        ITwilioService twilioService,
        IHubContext<ClientChatHub> clientChatHubContext)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _twilioService = twilioService;
        _clientChatHubContext = clientChatHubContext;
    }

    // Called when sending a message
    public async Task SendMessage(Guid conversationId, object message)
    {
        var content = ExtractContent(message);
        var attachmentUrl = ExtractAttachmentUrl(message);
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrl))
            return;

        var currentUser = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return;

        var isParticipant = await _unitOfWork.RepositoryOf<ConversationParticipant>()
            .Query()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUser.Id);

        if (!isParticipant)
            return;

        var entity = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = currentUser.Id,
            Content = content?.Trim() ?? string.Empty,
            AttachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl) ? null : attachmentUrl,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _unitOfWork.RepositoryOf<Message>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        await TrySendClientSmsAsync(conversationId, entity.Id, content, attachmentUrl);

        await SendToClientHubAsync(conversationId, entity);

        var dto = await BuildMessageDtoAsync(entity, currentUser.Id, true);
        await Clients.Caller.SendAsync("ReceiveMessage", dto);

        var otherDto = await BuildMessageDtoAsync(entity, currentUser.Id, false);
        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("ReceiveMessage", otherDto);
    }

    public override async Task OnConnectedAsync()
    {
        // Optionally handle user auth or logging
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    private async Task<User?> ResolveCurrentUserAsync()
    {
        var firebaseUid = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return null;

        var userResult = await _userService.GetUserByFirebaseUid(firebaseUid);
        return userResult.IsSuccess ? userResult.Value : null;
    }

    private async Task<ChatMessageDto> BuildMessageDtoAsync(Message message, Guid senderId, bool isMine)
    {
        var employee = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.UserId == senderId);

        var senderName = employee?.FullName;
        var senderAvatarUrl = employee?.ProfilePictureUrl;

        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            senderName,
            senderAvatarUrl,
            isMine,
            message.IsRead);
    }

    private static string? ExtractContent(object? payload)
    {
        if (payload is null)
            return null;

        if (payload is string text)
            return text;

        if (payload is JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(json, "content", out var contentValue))
                    return contentValue;
                if (TryGetString(json, "text", out var textValue))
                    return textValue;
            }

            if (json.ValueKind == JsonValueKind.String)
                return json.GetString();
        }

        return payload.ToString();
    }

    private static string? ExtractAttachmentUrl(object? payload)
    {
        if (payload is null)
            return null;

        if (payload is JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(json, "attachmentUrl", out var attachmentValue))
                    return attachmentValue;
            }
        }

        return null;
    }

    private static bool TryGetString(JsonElement json, string propertyName, out string? value)
    {
        value = null;
        if (!json.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind != JsonValueKind.String)
            return false;

        value = property.GetString();
        return true;
    }

    private async Task SendToClientHubAsync(Guid conversationId, Message message)
    {
        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationClientId.HasValue);

        if (conversation is null)
            return;

        var clientDto = new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            "JobFlow Team",
            null,
            false,
            message.IsRead);

        await _clientChatHubContext.Clients.Group(conversationId.ToString())
            .SendAsync("ReceiveMessage", clientDto);
    }

    private async Task TrySendClientSmsAsync(Guid conversationId, Guid messageId, string? content, string? attachmentUrl)
    {
        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation?.OrganizationClientId is null)
            return;

        var client = await _unitOfWork.RepositoryOf<OrganizationClient>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == conversation.OrganizationClientId.Value);

        if (client is null || string.IsNullOrWhiteSpace(client.PhoneNumber))
            return;

        var smsBody = BuildSmsBody(content, attachmentUrl);
        if (string.IsNullOrWhiteSpace(smsBody))
            return;

        try
        {
            await _twilioService.SendTextMessage(new TwilioModel
            {
                RecipientPhoneNumber = client.PhoneNumber,
                Message = smsBody
            });

            await Clients.Caller.SendAsync("SmsStatus", new
            {
                conversationId,
                messageId,
                status = "sent",
                to = client.PhoneNumber
            });
        }
        catch
        {
            await Clients.Caller.SendAsync("SmsStatus", new
            {
                conversationId,
                messageId,
                status = "failed",
                to = client.PhoneNumber
            });
        }
    }

    public async Task Typing(Guid conversationId, bool isTyping)
    {
        var currentUser = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return;

        var isParticipant = await _unitOfWork.RepositoryOf<ConversationParticipant>()
            .Query()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUser.Id);

        if (!isParticipant)
            return;

        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("Typing", new
        {
            conversationId,
            isTyping,
            senderType = "org"
        });

        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationClientId.HasValue);

        if (conversation is not null)
        {
            await _clientChatHubContext.Clients.Group(conversationId.ToString()).SendAsync("Typing", new
            {
                conversationId,
                isTyping,
                senderType = "org"
            });
        }
    }

    private static string BuildSmsBody(string? content, string? attachmentUrl)
    {
        var message = content?.Trim() ?? string.Empty;
        var attachment = attachmentUrl?.Trim();

        if (!string.IsNullOrWhiteSpace(attachment))
        {
            if (string.IsNullOrWhiteSpace(message))
                message = attachment;
            else
                message = $"{message}\n{attachment}";
        }

        return message;
    }

}