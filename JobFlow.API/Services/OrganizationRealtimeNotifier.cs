using JobFlow.API.Hubs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Services;

public class OrganizationRealtimeNotifier : IOrganizationRealtimeNotifier
{
    private readonly IHubContext<NotifierHub> _notifierHub;

    public OrganizationRealtimeNotifier(IHubContext<NotifierHub> notifierHub)
    {
        _notifierHub = notifierHub;
    }

    public async Task NotifyJobStatusChangedAsync(Guid organizationId, Guid jobId, string jobTitle, JobLifecycleStatus status)
    {
        await _notifierHub.Clients.Group($"org:{organizationId}:dashboard")
            .SendAsync("JobStatusChanged", new
            {
                jobId,
                jobTitle,
                status = (int)status,
                statusName = status.ToString()
            });
    }

    public async Task NotifyAssignmentChangedAsync(Guid organizationId, Guid jobId, Guid assignmentId)
    {
        await _notifierHub.Clients.Group($"org:{organizationId}:dashboard")
            .SendAsync("AssignmentChanged", new
            {
                jobId,
                assignmentId
            });
    }

    public async Task NotifyEstimateStatusChangedAsync(Guid organizationId, Guid estimateId, string status)
    {
        await _notifierHub.Clients.Group($"org:{organizationId}:dashboard")
            .SendAsync("EstimateStatusChanged", new
            {
                estimateId,
                status
            });
    }
}
