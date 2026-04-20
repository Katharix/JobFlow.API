using System.Security.Claims;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Hubs;

/// <summary>
/// Real-time SignalR hub for the Support Chat feature.
/// Customers connect anonymously (identified by sessionId).
/// Reps connect with a Firebase JWT and join the shared "reps" group.
/// </summary>
public class SupportChatHub : Hub
{
    private readonly ISupportChatService _chatService;

    public SupportChatHub(ISupportChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>Adds the caller to the named session group (used by both customer and rep).</summary>
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    /// <summary>Adds the caller to the shared "reps" group (all authenticated reps).</summary>
    public async Task JoinRepGroup()
    {
        if (!IsRep()) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, "reps");
    }

    /// <summary>Removes the caller from the named session group.</summary>
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    /// <summary>Saves a message via the service and broadcasts it to the session group.</summary>
    public async Task SendMessage(string sessionId, string content)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid)) return;

        var senderName = GetSenderName();
        var senderRole = IsRep() ? SupportChatSenderRole.Rep : SupportChatSenderRole.Customer;

        var request = new SupportChatSendMessageRequest(
            sessionGuid,
            GetRepId(),
            senderName,
            senderRole,
            content);

        var result = await _chatService.SendMessageAsync(request);
        if (result.IsSuccess)
        {
            await Clients.Group($"session-{sessionId}").SendAsync("ReceiveMessage", result.Value);
        }
    }

    /// <summary>Broadcasts a typing-started indicator to everyone else in the session group.</summary>
    public async Task StartTyping(string sessionId)
    {
        var senderName = GetSenderName();
        await Clients.OthersInGroup($"session-{sessionId}").SendAsync("UserTyping", new
        {
            senderName,
            isTyping = true
        });
    }

    /// <summary>Broadcasts a typing-stopped indicator to everyone else in the session group.</summary>
    public async Task StopTyping(string sessionId)
    {
        var senderName = GetSenderName();
        await Clients.OthersInGroup($"session-{sessionId}").SendAsync("UserTyping", new
        {
            senderName,
            isTyping = false
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool IsRep()
    {
        return Context.User?.IsInRole(JobFlow.Domain.Enums.UserRoles.KatharixAdmin) == true
            || Context.User?.IsInRole(JobFlow.Domain.Enums.UserRoles.KatharixEmployee) == true;
    }

    private Guid? GetRepId()
    {
        var uid = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(uid, out var id) ? id : null;
    }

    private string GetSenderName()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Name)
            ?? Context.User?.FindFirstValue("name")
            ?? "Support";
    }
}
