using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IEstimateService
{
    Task<Result<EstimateDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<EstimateDto>>> GetByOrganizationAsync(Guid organizationId);

    Task<Result<EstimateDto>> CreateAsync(CreateEstimateRequest request);
    Task<Result<EstimateDto>> UpdateAsync(Guid id, UpdateEstimateRequest request);
    Task<Result> DeleteAsync(Guid id);

    Task<Result<EstimateDto>> SendAsync(Guid id, SendEstimateRequest request);

    Task<Result<EstimateDto>> GetByPublicTokenAsync(string token);

    Task<Result<byte[]>> GetPublicPdfAsync(string token);
}