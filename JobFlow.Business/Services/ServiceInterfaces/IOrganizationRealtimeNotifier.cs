using JobFlow.Domain.Enums;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationRealtimeNotifier
{
    Task NotifyJobStatusChangedAsync(Guid organizationId, Guid jobId, string jobTitle, JobLifecycleStatus status);
    Task NotifyAssignmentChangedAsync(Guid organizationId, Guid jobId, Guid assignmentId);
    Task NotifyEstimateStatusChangedAsync(Guid organizationId, Guid estimateId, string status);
}
