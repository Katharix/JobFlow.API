using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Hubs;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/client-hub")]
[Authorize(AuthenticationSchemes = "ClientPortalJwt", Policy = "OrganizationClientOnly")]
public class ClientHubController : ControllerBase
{
    private readonly ILogger<ClientHubController> _logger;
    private readonly IEstimateService _estimates;
    private readonly IEstimateRevisionService _estimateRevisions;
    private readonly IInvoiceService _invoices;
    private readonly IOrganizationClientService _clients;
    private readonly IHubContext<NotifierHub> _hubContext;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<ClientChatHub> _clientChatHubContext;
    private readonly IUnitOfWork _unitOfWork;

    public ClientHubController(
        ILogger<ClientHubController> logger,
        IEstimateService estimates,
        IEstimateRevisionService estimateRevisions,
        IInvoiceService invoices,
        IOrganizationClientService clients,
        IHubContext<NotifierHub> hubContext,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<ClientChatHub> clientChatHubContext,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _estimates = estimates;
        _estimateRevisions = estimateRevisions;
        _invoices = invoices;
        _clients = clients;
        _hubContext = hubContext;
        _chatHubContext = chatHubContext;
        _clientChatHubContext = clientChatHubContext;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("me")]
    public async Task<IResult> Me()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        return Results.Ok(clientResult.Value);
    }

    [HttpPut("me")]
    public async Task<IResult> UpdateMe([FromBody] UpdateOrganizationClientRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        var client = clientResult.Value;
        if (client.OrganizationId != organizationId)
            return Results.Unauthorized();

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        client.EmailAddress = request.EmailAddress;
        client.PhoneNumber = request.PhoneNumber;
        client.Address1 = request.Address1;
        client.Address2 = request.Address2;
        client.City = request.City;
        client.State = request.State;
        client.ZipCode = request.ZipCode;

        var upsert = await _clients.UpsertClient(client);
        return upsert.IsSuccess ? Results.Ok(upsert.Value) : upsert.ToProblemDetails();
    }

    [HttpGet("chat/conversation")]
    public async Task<IResult> GetChatConversation()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        var conversation = await FindOrCreateClientConversationAsync(orgClientId, organizationId);

        var lastMessage = await _unitOfWork.RepositoryOf<Message>()
            .Query()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync();

        var org = await _unitOfWork.RepositoryOf<Organization>()
            .Query()
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        var name = org?.OrganizationName ?? "Your Team";
        var lastMessageDto = lastMessage is null
            ? null
            : MapClientHubMessage(lastMessage, clientResult.Value);

        var dto = new ChatConversationDto(
            conversation.Id,
            name,
            null,
            "Organization",
            "online",
            0,
            lastMessageDto);

        return Results.Ok(dto);
    }

    [HttpGet("chat/messages")]
    public async Task<IResult> GetChatMessages([FromQuery] Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OrganizationClientId == orgClientId);

        if (conversation is null)
            return Results.NotFound();

        var messages = await _unitOfWork.RepositoryOf<Message>()
            .Query()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = messages
            .OrderBy(m => m.SentAt)
            .Select(m => MapClientHubMessage(m, clientResult.Value))
            .ToList();

        return Results.Ok(result);
    }

    [HttpPost("chat/messages")]
    public async Task<IResult> CreateChatMessage([FromBody] CreateMessageRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        if (request.ConversationId == Guid.Empty)
            return Results.BadRequest("ConversationId is required.");

        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            return Results.BadRequest("Message content or attachment is required.");

        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.OrganizationClientId == orgClientId);

        if (conversation is null)
            return Results.NotFound();

        var senderName = clientResult.Value.ClientFullName().Trim();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = null,
            Content = request.Content?.Trim() ?? string.Empty,
            AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            ExternalSenderName = senderName,
            ExternalSenderType = "client",
            ExternalSenderPhone = clientResult.Value.PhoneNumber
        };

        await _unitOfWork.RepositoryOf<Message>().AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapClientHubMessage(message, clientResult.Value);
        await _chatHubContext.Clients.Group(conversation.Id.ToString()).SendAsync("ReceiveMessage", dto);
        await _clientChatHubContext.Clients.Group(conversation.Id.ToString()).SendAsync("ReceiveMessage", dto);

        return Results.Ok(dto);
    }

    [HttpPost("chat/read")]
    public async Task<IResult> MarkChatRead([FromBody] ClientHubReadRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        if (request.ConversationId == Guid.Empty)
            return Results.BadRequest("ConversationId is required.");

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.OrganizationClientId == orgClientId);

        if (conversation is null)
            return Results.NotFound();

        var messages = await _unitOfWork.RepositoryOf<Message>()
            .Query()
            .Where(m => m.ConversationId == request.ConversationId && m.SenderId.HasValue && !m.IsRead)
            .ToListAsync();

        if (messages.Count == 0)
            return Results.Ok(new { updated = 0 });

        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        await _unitOfWork.SaveChangesAsync();

        var readIds = messages.Select(m => m.Id).ToList();
        await _chatHubContext.Clients.Group(request.ConversationId.ToString()).SendAsync("ReadReceipt", new
        {
            conversationId = request.ConversationId,
            messageIds = readIds
        });

        await _clientChatHubContext.Clients.Group(request.ConversationId.ToString()).SendAsync("ReadReceipt", new
        {
            conversationId = request.ConversationId,
            messageIds = readIds
        });

        return Results.Ok(new { updated = readIds.Count });
    }

    [HttpGet("estimates")]
    public async Task<IResult> GetMyEstimates()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var orgEstimates = await _estimates.GetByOrganizationAsync(organizationId);
        if (!orgEstimates.IsSuccess)
            return orgEstimates.ToProblemDetails();

        var mine = orgEstimates.Value.Where(x => x.OrganizationClientId == orgClientId);
        return Results.Ok(mine);
    }

    [HttpGet("estimates/{id:guid}")]
    public async Task<IResult> GetMyEstimateById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var estimate = result.Value;
        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != orgClientId)
            return Results.NotFound();

        return Results.Ok(estimate);
    }

    [HttpPost("estimates/{id:guid}/accept")]
    public async Task<IResult> AcceptEstimate(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.AcceptAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("estimates/{id:guid}/decline")]
    public async Task<IResult> DeclineEstimate(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.DeclineAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("estimates/{id:guid}/revision-requests")]
    [RequestSizeLimit(55_000_000)]
    public async Task<IResult> RequestEstimateRevision(Guid id, [FromForm] CreateEstimateRevisionFormRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var attachments = new List<EstimateRevisionAttachmentUpload>();

        if (request.Attachments is not null)
        {
            foreach (var file in request.Attachments)
            {
                if (file.Length <= 0)
                    continue;

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                attachments.Add(new EstimateRevisionAttachmentUpload(
                    file.FileName,
                    file.ContentType,
                    stream.ToArray(),
                    file.Length));
            }
        }

        var result = await _estimateRevisions.CreateAsync(
            id,
            organizationId,
            orgClientId,
            new CreateEstimateRevisionRequest(request.Message ?? string.Empty, attachments));

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        await _hubContext.Clients.Group($"org:{organizationId}:dashboard")
            .SendAsync("EstimateRevisionRequested", new
            {
                estimateId = id,
                revisionRequestId = result.Value.Id,
                revisionNumber = result.Value.RevisionNumber,
                requestedAt = result.Value.RequestedAt,
                message = result.Value.RequestMessage
            });

        return Results.Ok(result.Value);
    }

    [HttpGet("estimates/{id:guid}/revision-requests")]
    public async Task<IResult> GetEstimateRevisionRequests(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimateRevisions.GetByEstimateAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("estimates/{estimateId:guid}/revision-requests/{revisionRequestId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IResult> DownloadEstimateRevisionAttachment(Guid estimateId, Guid revisionRequestId, Guid attachmentId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimateRevisions.GetAttachmentAsync(
            estimateId,
            revisionRequestId,
            attachmentId,
            organizationId,
            orgClientId);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("invoices")]
    public async Task<IResult> GetMyInvoices()
    {
        var orgClientId = HttpContext.GetUserId();
        var result = await _invoices.GetInvoicesByClientAsync(orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value.ToDto()) : result.ToProblemDetails();
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IResult> GetMyInvoiceById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        _logger.LogInformation(
            "ClientHub invoice detail requested. InvoiceId={InvoiceId} OrgId={OrgId} ClientId={ClientId}",
            id,
            organizationId,
            orgClientId);

        var result = await _invoices.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "ClientHub invoice not found. InvoiceId={InvoiceId} OrgId={OrgId} ClientId={ClientId}",
                id,
                organizationId,
                orgClientId);
            return result.ToProblemDetails();
        }

        var invoice = result.Value;
        if (invoice.OrganizationId != organizationId || invoice.OrganizationClientId != orgClientId)
        {
            _logger.LogWarning(
                "ClientHub invoice mismatch. InvoiceId={InvoiceId} InvoiceOrgId={InvoiceOrgId} InvoiceClientId={InvoiceClientId} OrgId={OrgId} ClientId={ClientId}",
                id,
                invoice.OrganizationId,
                invoice.OrganizationClientId,
                organizationId,
                orgClientId);
            return Results.NotFound();
        }

        return Results.Ok(invoice.ToDto());
    }

    private async Task<Conversation> FindOrCreateClientConversationAsync(Guid orgClientId, Guid organizationId)
    {
        var conversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.OrganizationClientId == orgClientId);

        if (conversation is not null)
            return conversation;

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            OrganizationClientId = orgClientId
        };

        var participantIds = await GetOrganizationUserIdsAsync(organizationId);
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
        return conversation;
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

    private static ChatMessageDto MapClientHubMessage(Message message, OrganizationClient client)
    {
        var isMine = !message.SenderId.HasValue;
        var senderName = isMine ? client.ClientFullName().Trim() : "JobFlow Team";

        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.AttachmentUrl,
            message.SentAt,
            senderName,
            null,
            isMine,
            message.IsRead);
    }
}

public record UpdateOrganizationClientRequest(
    string? FirstName,
    string? LastName,
    string? EmailAddress,
    string? PhoneNumber,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode);

public record CreateEstimateRevisionFormRequest(
    string? Message,
    List<IFormFile>? Attachments);

public record ClientHubReadRequest(Guid ConversationId);
