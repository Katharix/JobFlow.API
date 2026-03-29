using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IWorkflowSettingsService
{
    Task<Result<List<WorkflowStatusDto>>> GetJobLifecycleStatusesAsync(Guid organizationId);
    Task<Result<List<WorkflowStatusDto>>> UpsertJobLifecycleStatusesAsync(Guid organizationId, List<WorkflowStatusUpsertRequestDto> statuses);
    Task<Result<Dictionary<JobLifecycleStatus, string>>> GetJobLifecycleLabelMapAsync(Guid organizationId);
}
