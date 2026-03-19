using System.Security.Claims;
using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;

    public ChatController(IUserService userService, IUnitOfWork unitOfWork)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
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

        var results = conversations
            .Select(conversation => MapConversation(conversation, currentUser.Id, employeeLookup, userLookup))
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

        var senderIds = paged.Select(m => m.SenderId).Distinct().ToList();

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

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Message content is required.");

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
                return Ok(MapConversation(existing, currentUser.Id, employeeLookupExisting, userLookupExisting));
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

        return Ok(MapConversation(conversation, currentUser.Id, employeeLookup, userLookup));
    }

    private static ChatConversationDto MapConversation(
        Conversation conversation,
        Guid currentUserId,
        IDictionary<Guid, Employee> employeeLookup,
        IDictionary<Guid, User> userLookup)
    {
        var otherParticipant = conversation.Participants
            .Select(p => p.UserId)
            .FirstOrDefault(id => id != currentUserId);

        var (name, role, avatar) = ResolveParticipantDisplay(otherParticipant, employeeLookup, userLookup);

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
        var (name, _, avatar) = ResolveParticipantDisplay(message.SenderId, employeeLookup, userLookup);

        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            name,
            avatar,
            message.SenderId == currentUserId);
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
}
