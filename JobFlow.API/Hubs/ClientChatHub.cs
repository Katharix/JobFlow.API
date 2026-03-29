using System.Security.Claims;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Hubs;

[Authorize(AuthenticationSchemes = "ClientPortalJwt", Policy = "OrganizationClientOnly")]
public class ClientChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<ChatHub> _orgChatHubContext;

    public ClientChatHub(IUnitOfWork unitOfWork, IHubContext<ChatHub> orgChatHubContext)
    {
        _unitOfWork = unitOfWork;
        _orgChatHubContext = orgChatHubContext;
    }

    public override async Task OnConnectedAsync()
    {
        var orgClientId = GetOrganizationClientId();
        if (orgClientId != Guid.Empty)
        {
            var conversationId = await _unitOfWork.RepositoryOf<Conversation>()
                .Query()
                .Where(c => c.OrganizationClientId == orgClientId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (conversationId != Guid.Empty)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var orgClientId = GetOrganizationClientId();
        if (orgClientId == Guid.Empty)
            return;

        var isClientConversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .AnyAsync(c => c.Id == conversationId && c.OrganizationClientId == orgClientId);

        if (!isClientConversation)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task Typing(Guid conversationId, bool isTyping)
    {
        var orgClientId = GetOrganizationClientId();
        if (orgClientId == Guid.Empty)
            return;

        var isClientConversation = await _unitOfWork.RepositoryOf<Conversation>()
            .Query()
            .AnyAsync(c => c.Id == conversationId && c.OrganizationClientId == orgClientId);

        if (!isClientConversation)
            return;

        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("Typing", new
        {
            conversationId,
            isTyping,
            senderType = "client"
        });

        await _orgChatHubContext.Clients.Group(conversationId.ToString()).SendAsync("Typing", new
        {
            conversationId,
            isTyping,
            senderType = "client"
        });
    }

    private Guid GetOrganizationClientId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
