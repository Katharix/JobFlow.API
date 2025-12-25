using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Hubs;

public class ChatHub : Hub
{
    // Called when sending a message
    public async Task SendMessage(Guid conversationId, object message)
    {
        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);
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
}