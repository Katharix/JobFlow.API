using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEstimateRevisionService
{
    Task<Result<EstimateRevisionRequestDto>> CreateAsync(
        Guid estimateId,
        Guid organizationId,
        Guid organizationClientId,
        CreateEstimateRevisionRequest request);

    Task<Result<IReadOnlyList<EstimateRevisionRequestDto>>> GetByEstimateAsync(
        Guid estimateId,
        Guid organizationId,
        Guid organizationClientId);

    Task<Result<EstimateRevisionAttachmentDownloadDto>> GetAttachmentAsync(
        Guid estimateId,
        Guid revisionRequestId,
        Guid attachmentId,
        Guid organizationId,
        Guid organizationClientId);
}
