using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrganizationBrandingService
    {
        Task<Result<OrganizationBranding>> GetByOrganizationIdAsync(Guid organizationId);
        Task<Result<OrganizationBranding>> CreateOrUpdateAsync(OrganizationBranding model);
    }
}
