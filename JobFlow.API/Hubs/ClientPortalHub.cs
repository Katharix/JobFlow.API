using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Hubs;

[Authorize(AuthenticationSchemes = "ClientPortalJwt", Policy = "OrganizationClientOnly")]
public class ClientPortalHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var clientId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(clientId, out var orgClientId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client:{orgClientId}");
        }

        await base.OnConnectedAsync();
    }
}
