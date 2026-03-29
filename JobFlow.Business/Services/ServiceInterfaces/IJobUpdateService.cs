using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IJobUpdateService
{
    Task<Result<JobUpdateDto>> CreateAsync(Guid jobId, Guid organizationId, CreateJobUpdateRequest request);
    Task<Result<IReadOnlyList<JobUpdateDto>>> GetByJobAsync(Guid jobId, Guid organizationId);
    Task<Result<IReadOnlyList<JobUpdateDto>>> GetByJobForClientAsync(Guid jobId, Guid organizationId, Guid organizationClientId);
    Task<Result<JobUpdateAttachmentDownloadDto>> GetAttachmentAsync(
        Guid jobId,
        Guid updateId,
        Guid attachmentId,
        Guid organizationId,
        Guid? organizationClientId = null);
}
