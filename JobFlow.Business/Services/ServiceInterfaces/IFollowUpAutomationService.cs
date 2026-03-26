using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IFollowUpAutomationService
{
    Task<Result<IReadOnlyList<FollowUpSequenceDto>>> GetSequencesAsync(Guid organizationId, FollowUpSequenceType? sequenceType = null);
    Task<Result<FollowUpSequenceDto>> UpsertSequenceAsync(Guid organizationId, FollowUpSequenceUpsertRequestDto request);
    Task<Result<IReadOnlyList<FollowUpRunDto>>> GetEstimateRunsAsync(Guid organizationId, Guid estimateId);

    Task<Result> StartEstimateSequenceAsync(Guid organizationId, Guid estimateId, Guid organizationClientId);
    Task<Result> StopEstimateSequencesOnClientReplyAsync(Guid organizationId, Guid organizationClientId);
    Task<Result> StopEstimateSequenceAsync(Guid estimateId, FollowUpStopReason reason);
    Task<Result> ExecuteRunStepAsync(Guid runId);
}
