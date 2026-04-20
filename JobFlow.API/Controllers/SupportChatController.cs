using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/supporthub/chat")]
public class SupportChatController : ControllerBase
{
    private readonly ISupportChatService _chatService;
    private readonly IUserService _userService;
    private readonly IHubContext<Hubs.SupportChatHub> _hubContext;

    public SupportChatController(
        ISupportChatService chatService,
        IUserService userService,
        IHubContext<Hubs.SupportChatHub> hubContext)
    {
        _chatService = chatService;
        _userService = userService;
        _hubContext = hubContext;
    }

    /// <summary>Customer joins the support queue. Returns a session ID and estimated wait.</summary>
    [HttpPost("queue/join")]
    [AllowAnonymous]
    public async Task<IResult> JoinQueue([FromBody] SupportChatJoinQueueRequest request)
    {
        var result = await _chatService.JoinQueueAsync(request.CustomerName, request.CustomerEmail);
        if (result.IsFailure) return result.ToProblemDetails();

        await _hubContext.Clients.Group("reps").SendAsync("QueueUpdated");
        return Results.Ok(result.Value);
    }

    /// <summary>Returns the current support queue (reps only).</summary>
    [HttpGet("queue")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> GetQueue()
    {
        var result = await _chatService.GetQueueAsync();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Rep picks a specific customer from the queue by sessionId.</summary>
    [HttpPost("sessions/{sessionId}/pick")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> PickCustomer(Guid sessionId)
    {
        var firebaseUid = HttpContext.GetFirebaseUid();
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return Results.Unauthorized();

        var userResult = await _userService.GetUserByFirebaseUid(firebaseUid);
        if (userResult.IsFailure) return userResult.ToProblemDetails();

        var rep = userResult.Value;
        var repName = $"{rep.FirstName} {rep.LastName}".Trim() is { Length: > 0 } n ? n : rep.Email ?? "Support Rep";

        var result = await _chatService.PickCustomerAsync(sessionId, rep.Id, repName);
        if (result.IsFailure) return result.ToProblemDetails();

        await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("AgentJoined", new { agentName = repName });
        await _hubContext.Clients.Group("reps").SendAsync("QueueUpdated");

        return Results.Ok(result.Value);
    }

    /// <summary>Returns all messages for a session. Accessible without authentication (customer view).</summary>
    [HttpGet("sessions/{sessionId}/messages")]
    [AllowAnonymous]
    public async Task<IResult> GetMessages(Guid sessionId)
    {
        var result = await _chatService.GetSessionMessagesAsync(sessionId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Saves a chat message and broadcasts it to the session group via SignalR.</summary>
    [HttpPost("messages")]
    [AllowAnonymous]
    public async Task<IResult> SendMessage([FromBody] SupportChatSendMessageRequest request)
    {
        var result = await _chatService.SendMessageAsync(request);
        if (result.IsFailure) return result.ToProblemDetails();

        await _hubContext.Clients
            .Group($"session-{request.SessionId}")
            .SendAsync("ReceiveMessage", result.Value);

        return Results.Ok(result.Value);
    }

    /// <summary>Uploads a file attachment for a session. Returns the file metadata.</summary>
    [HttpPost("sessions/{sessionId}/upload")]
    [AllowAnonymous]
    public async Task<IResult> UploadFile(Guid sessionId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("No file provided.");

        await using var stream = file.OpenReadStream();
        var result = await _chatService.UploadFileAsync(stream, file.FileName, file.ContentType);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Rep closes a session and notifies all participants.</summary>
    [HttpPost("sessions/{sessionId}/close")]
    [Authorize(Roles = $"{UserRoles.KatharixAdmin},{UserRoles.KatharixEmployee}")]
    public async Task<IResult> CloseSession(Guid sessionId)
    {
        var result = await _chatService.CloseSessionAsync(sessionId);
        if (result.IsFailure) return result.ToProblemDetails();

        await _hubContext.Clients.Group($"session-{sessionId}").SendAsync("SessionClosed");
        await _hubContext.Clients.Group("reps").SendAsync("QueueUpdated");

        return Results.Ok();
    }

    /// <summary>Validates a customer email before allowing chat access.</summary>
    [HttpGet("auth/validate")]
    [AllowAnonymous]
    public async Task<IResult> ValidateCustomer([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest("Email is required.");

        var result = await _chatService.ValidateCustomerAsync(email);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}
