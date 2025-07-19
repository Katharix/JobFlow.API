using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrganizationTypeService
    {
        Task<Result<IEnumerable<OrganizationType>>> GetTypes();
        Task<Result<OrganizationType>> GetTypeById(Guid organizationTypeId);
        Task<Result<OrganizationType>> UpsertOrganizationType(OrganizationType model);
        Task<Result<IEnumerable<OrganizationType>>> UpsertOrganizationList(IEnumerable<OrganizationType> modelList);
        Task<Result> DeleteOrganizationType(Guid organizationTypeId);
        Task<Result> DeleteMultipleOrganizationTypes(IEnumerable<Guid> idList);
    }
}
