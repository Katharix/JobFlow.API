using System.Security.Claims;
using JobFlow.API.Extensions;
using JobFlow.API.Hubs;
using JobFlow.API.Models;
using JobFlow.Business.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwilioService _twilioService;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<ClientChatHub> _clientChatHubContext;

    public ChatController(
        IUserService userService,
        IUnitOfWork unitOfWork,
        ITwilioService twilioService,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<ClientChatHub> clientChatHubContext)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _twilioService = twilioService;
        _chatHubContext = chatHubContext;
        _clientChatHubContext = clientChatHubContext;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var (currentUser, organizationId, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        var conversations = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(p => p.UserId == currentUser.Id))
            .ToListAsync();

        var participantIds = conversations
            .SelectMany(c => c.Participants)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();

        var employeeLookup = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue && participantIds.Contains(e.UserId.Value))
            .ToDictionaryAsync(e => e.UserId!.Value);

        var userLookup = await _unitOfWork.RepositoryOf<User>()
            .Query()
            .Where(u => participantIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var clientIds = conversations
            .Where(c => c.OrganizationClientId.HasValue)
            .Select(c => c.OrganizationClientId!.Value)
            .Distinct()
            .ToList();

        var clientLookup = clientIds.Count == 0
            ? new Dictionary<Guid, OrganizationClient>()
            : await _unitOfWork.RepositoryOf<OrganizationClient>()
                .Query()
                .Where(c => clientIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

        var results = conversations
            .Select(conversation => MapConversation(conversation, currentUser.Id, employeeLookup, userLookup, clientLookup))
            .OrderByDescending(c => c.LastMessage?.SentAt ?? DateTime.MinValue)
            .ToList();

        return Ok(results);
    }

    [HttpGet("messages/{conversationId:guid}")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (currentUser, organizationId, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var isParticipant = await _unitOfWork.RepositoryOf<ConversationParticipant>()
            .Query()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUser.Id);

        if (!isParticipant)
            return Forbid();

        var messageQuery = _unitOfWork.RepositoryOf<Message>()
            .Query()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt);

        var paged = await messageQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var senderIds = paged
            .Where(m => m.SenderId.HasValue)
            .Select(m => m.SenderId!.Value)
            .Distinct()
            .ToList();

        var employeeLookup = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue && senderIds.Contains(e.UserId.Value))
            .ToDictionaryAsync(e => e.UserId!.Value);

        var userLookup = await _unitOfWork.RepositoryOf<User>()
            .Query()
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var messages = paged
            .OrderBy(m => m.SentAt)
            .Select(message => MapMessage(message, currentUser.Id, employeeLookup, userLookup))
            .ToList();

        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
    {
        var (currentUser, organizationId, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        if (request.ConversationId == Guid.Empty)
            return BadRequest("ConversationId is required.");

        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            return BadRequest("Message content or attachment is required.");

        var isParticipant = await _unitOfWork.RepositoryOf<ConversationParticipant>()
            .Query()
            .AnyAsync(p => p.ConversationId == request.ConversationId && p.UserId == currentUser.Id);

        if (!isParticipant)
            return Forbid();

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = currentUser.Id,
            Content = request.Content.Trim(),
            AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _unitOfWork.RepositoryOf<Message>().AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        await TrySendClientSmsAsync(request.ConversationId, request.Content, request.AttachmentUrl);
        await SendToClientHubAsync(request.ConversationId, message);

        var employeeLookup = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue && e.UserId.Value == currentUser.Id)
            .ToDictionaryAsync(e => e.UserId!.Value);

        var userLookup = new Dictionary<Guid, User>
        {
            { currentUser.Id, currentUser }
        };

        var dto = MapMessage(message, currentUser.Id, employeeLookup, userLookup);
        return Ok(dto);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkConversationRead(Guid conversationId)
    {
        var (currentUser, _, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        var isParticipant = await _unitOfWork.RepositoryOf<ConversationParticipant>()
            .Query()
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUser.Id);

        if (!isParticipant)
            return Forbid();

        var messages = await _unitOfWork.RepositoryOf<Message>()
            .Query()
            .Where(m => m.ConversationId == conversationId && m.SenderId != currentUser.Id && !m.IsRead)
            .ToListAsync();

        if (messages.Count == 0)
            return Ok();

        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        await _unitOfWork.SaveChangesAsync();

        var readIds = messages.Select(m => m.Id).ToList();
        await _chatHubContext.Clients.Group(conversationId.ToString()).SendAsync("ReadReceipt", new
        {
            conversationId,
            messageIds = readIds
        });

        await _clientChatHubContext.Clients.Group(conversationId.ToString()).SendAsync("ReadReceipt", new
        {
            conversationId,
            messageIds = readIds
        });
        return Ok();
    }


    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var (currentUser, organizationId, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        if (request.ParticipantIds is null || request.ParticipantIds.Count == 0)
            return BadRequest("At least one participant is required.");

        var participantGuids = request.ParticipantIds
            .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
            .Where(guid => guid != Guid.Empty)
            .Distinct()
            .ToList();

        if (participantGuids.Count == 0)
            return BadRequest("Participant ids must be valid GUIDs.");

        if (!participantGuids.Contains(currentUser.Id))
            participantGuids.Insert(0, currentUser.Id);

        var users = await _unitOfWork.RepositoryOf<User>()
            .Query()
            .Where(u => participantGuids.Contains(u.Id) && u.OrganizationId == organizationId)
            .ToListAsync();

        if (users.Count != participantGuids.Count)
            return BadRequest("All participants must belong to the current organization.");

        if (participantGuids.Count == 2)
        {
            var existing = await _unitOfWork.RepositoryOf<Conversation>()
                .Query()
                .Include(c => c.Participants)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Participants.Count == 2
                    && c.Participants.All(p => participantGuids.Contains(p.UserId)));

            if (existing is not null)
            {
                var employeeLookupExisting = await _unitOfWork.RepositoryOf<Employee>()
                    .Query()
                    .Include(e => e.Role)
                    .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue && participantGuids.Contains(e.UserId.Value))
                    .ToDictionaryAsync(e => e.UserId!.Value);

                var userLookupExisting = users.ToDictionary(u => u.Id);
                return Ok(MapConversation(existing, currentUser.Id, employeeLookupExisting, userLookupExisting, new Dictionary<Guid, OrganizationClient>()));
            }
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = null
        };

        foreach (var user in users)
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = user.Id
            });
        }

        await _unitOfWork.RepositoryOf<Conversation>().AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        var employeeLookup = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue && participantGuids.Contains(e.UserId.Value))
            .ToDictionaryAsync(e => e.UserId!.Value);

        var userLookup = users.ToDictionary(u => u.Id);

        return Ok(MapConversation(conversation, currentUser.Id, employeeLookup, userLookup, new Dictionary<Guid, OrganizationClient>()));
    }

    [HttpPost("conversations/client")]
    public async Task<IActionResult> CreateClientConversation([FromBody] CreateClientConversationRequest request)
    {
        var (currentUser, organizationId, firebaseUidResult) = await ResolveCurrentUserAsync();
        if (currentUser is null)
            return firebaseUidResult ?? Unauthorized();

        if (request.OrganizationClientId == Guid.Empty)
            return BadRequest("OrganizationClientId is required.");

        var client = await _unitOfWork.RepositoryOf<OrganizationClient>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == request.OrganizationClientId && c.OrganizationId == organizationId);

        if (client is null)
            return NotFound("Client not found for this organization.");

        var existing = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .Include(c => c.Participants)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.OrganizationClientId == request.OrganizationClientId);

        if (existing is not null)
        {
            var employeeLookupExisting = await _unitOfWork.RepositoryOf<Employee>()
                .Query()
                .Include(e => e.Role)
                .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue)
                .ToDictionaryAsync(e => e.UserId!.Value);

            var userLookupExisting = await _unitOfWork.RepositoryOf<User>()
                .Query()
                .Where(u => employeeLookupExisting.Keys.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            return Ok(MapConversation(existing, currentUser.Id, employeeLookupExisting, userLookupExisting,
                new Dictionary<Guid, OrganizationClient> { { client.Id, client } }));
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = null,
            OrganizationClientId = client.Id
        };

        var participantIds = await GetOrganizationUserIdsAsync(organizationId, currentUser.Id);
        foreach (var userId in participantIds)
        {
            conversation.Participants.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = userId
            });
        }

        await _unitOfWork.RepositoryOf<Conversation>().AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        var employeeLookup = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue)
            .ToDictionaryAsync(e => e.UserId!.Value);

        var userLookup = await _unitOfWork.RepositoryOf<User>()
            .Query()
            .Where(u => employeeLookup.Keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var clientLookup = new Dictionary<Guid, OrganizationClient> { { client.Id, client } };
        return Ok(MapConversation(conversation, currentUser.Id, employeeLookup, userLookup, clientLookup));
    }

    private static ChatConversationDto MapConversation(
        Conversation conversation,
        Guid currentUserId,
        IDictionary<Guid, Employee> employeeLookup,
        IDictionary<Guid, User> userLookup,
        IDictionary<Guid, OrganizationClient> clientLookup)
    {
        string? name = null;
        string? role = null;
        string? avatar = null;

        if (conversation.OrganizationClientId.HasValue
            && clientLookup.TryGetValue(conversation.OrganizationClientId.Value, out var client))
        {
            name = client.ClientFullName().Trim();
            role = "Client";
            avatar = null;
        }
        else
        {
            var otherParticipant = conversation.Participants
                .Select(p => p.UserId)
                .FirstOrDefault(id => id != currentUserId);

            var resolved = ResolveParticipantDisplay(otherParticipant, employeeLookup, userLookup);
            name = resolved.name;
            role = resolved.role;
            avatar = resolved.avatarUrl;
        }

        var lastMessage = conversation.Messages
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefault();

        var lastMessageDto = lastMessage is null
            ? null
            : MapMessage(lastMessage, currentUserId, employeeLookup, userLookup);

        var unreadCount = conversation.Messages.Count(m => m.SenderId != currentUserId && !m.IsRead);

        return new ChatConversationDto(
            conversation.Id,
            name ?? conversation.Title ?? "Conversation",
            avatar,
            role ?? "Team member",
            "online",
            unreadCount,
            lastMessageDto);
    }

    private static ChatMessageDto MapMessage(
        Message message,
        Guid currentUserId,
        IDictionary<Guid, Employee> employeeLookup,
        IDictionary<Guid, User> userLookup)
    {
        string? name = null;
        string? avatar = null;

        if (message.SenderId.HasValue)
        {
            var resolved = ResolveParticipantDisplay(message.SenderId.Value, employeeLookup, userLookup);
            name = resolved.name;
            avatar = resolved.avatarUrl;
        }
        else
        {
            name = message.ExternalSenderName;
            avatar = null;
        }

        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            name,
            avatar,
            message.SenderId.HasValue && message.SenderId.Value == currentUserId,
            message.IsRead);
    }

    private static (string? name, string? role, string? avatarUrl) ResolveParticipantDisplay(
        Guid userId,
        IDictionary<Guid, Employee> employeeLookup,
        IDictionary<Guid, User> userLookup)
    {
        if (employeeLookup.TryGetValue(userId, out var employee))
        {
            return (
                employee.FullName,
                employee.Role?.Name,
                employee.ProfilePictureUrl
            );
        }

        if (userLookup.TryGetValue(userId, out var user))
        {
            return (user.Email, null, null);
        }

        return (null, null, null);
    }

    private async Task<(User? user, Guid organizationId, IActionResult? errorResult)> ResolveCurrentUserAsync()
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return (null, Guid.Empty, Unauthorized());

        var userResult = await _userService.GetUserByFirebaseUid(firebaseUid);
        if (!userResult.IsSuccess)
            return (null, Guid.Empty, Forbid());

        var organizationId = HttpContext.GetOrganizationId();
        return (userResult.Value, organizationId, null);
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

    private async Task<List<Guid>> GetOrganizationUserIdsAsync(Guid organizationId, Guid fallbackUserId)
    {
        var userIds = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue)
            .Select(e => e.UserId!.Value)
            .Distinct()
            .ToListAsync();

        if (!userIds.Contains(fallbackUserId))
            userIds.Add(fallbackUserId);

        return userIds;
    }

    private async Task TrySendClientSmsAsync(Guid conversationId, string? content, string? attachmentUrl)
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

        await _twilioService.SendTextMessage(new TwilioModel
        {
            RecipientPhoneNumber = client.PhoneNumber,
            Message = smsBody
        });
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
