using JobFlow.API.Hubs;
using JobFlow.API.Models;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/chat/sms")]
public class ChatSmsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHubContext<ClientChatHub> _clientHubContext;

    public ChatSmsController(
        IUnitOfWork unitOfWork,
        IHubContext<ChatHub> hubContext,
        IHubContext<ClientChatHub> clientHubContext)
    {
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _clientHubContext = clientHubContext;
    }

    [HttpPost("inbound")]
    [AllowAnonymous]
    public async Task<IActionResult> Inbound([FromForm] TwilioInboundSmsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.From))
            return TwilioOk();

        var fromNormalized = NormalizePhone(request.From);
        if (string.IsNullOrWhiteSpace(fromNormalized))
            return TwilioOk();

        var clients = await _unitOfWork.RepositoryOf<OrganizationClient>()
            .Query()
            .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
            .ToListAsync();

        var client = clients.FirstOrDefault(c => NormalizePhone(c.PhoneNumber) == fromNormalized);
        if (client is null)
            return TwilioOk();

        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.OrganizationClientId == client.Id);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                OrganizationClientId = client.Id
            };

            var participantIds = await GetOrganizationUserIdsAsync(client.OrganizationId);
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
        }

        var content = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return TwilioOk();

        var senderName = client.ClientFullName().Trim();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = null,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            ExternalSenderName = senderName,
            ExternalSenderType = "client",
            ExternalSenderPhone = client.PhoneNumber
        };

        await _unitOfWork.RepositoryOf<Message>().AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            senderName,
            null,
            false,
            message.IsRead);

        await _hubContext.Clients.Group(conversation.Id.ToString()).SendAsync("ReceiveMessage", dto);
        await _clientHubContext.Clients.Group(conversation.Id.ToString()).SendAsync("ReceiveMessage", dto);

        return TwilioOk();
    }

    private async Task<List<Guid>> GetOrganizationUserIdsAsync(Guid organizationId)
    {
        var userIds = await _unitOfWork.RepositoryOf<Employee>()
            .Query()
            .Where(e => e.OrganizationId == organizationId && e.UserId.HasValue)
            .Select(e => e.UserId!.Value)
            .Distinct()
            .ToListAsync();

        return userIds;
    }

    private static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits;
    }

    private static ContentResult TwilioOk()
    {
        return new ContentResult
        {
            Content = "<Response></Response>",
            ContentType = "text/xml",
            StatusCode = 200
        };
    }
}

public record TwilioInboundSmsRequest(string? From, string? To, string? Body);
