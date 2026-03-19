using System.Security.Claims;
using System.Text.Json;
using JobFlow.API.Models;
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

    public ChatHub(IUserService userService, IUnitOfWork unitOfWork)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
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
            isMine);
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

}