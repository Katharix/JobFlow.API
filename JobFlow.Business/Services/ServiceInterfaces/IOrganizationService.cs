using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrganizationService
    {
        Task<Result<Organization>> GetOrganiztionById(Guid OrgId);
        Task<Result<IEnumerable<Organization>>> GetAllOrganizations();
        Task<Result<Organization>> UpsertOrganization(Organization model);
        Task<Result> DeleteOrganization(Guid organizationId);
    }
}
