using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Hubs;

[Authorize]
public class NotifierHub : Hub
{
    public async Task JoinOrganizationDashboard()
    {
        var organizationId = Context.User?.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(organizationId, out var orgId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org:{orgId}:dashboard");
    }

    public async Task LeaveOrganizationDashboard()
    {
        var organizationId = Context.User?.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(organizationId, out var orgId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org:{orgId}:dashboard");
    }
}
