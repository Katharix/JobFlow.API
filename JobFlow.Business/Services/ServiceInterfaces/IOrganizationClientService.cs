using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationClientService
{
    Task<Result> DeleteClient(Guid clientId);
    Task<Result<OrganizationClient>> GetClientById(Guid clientId);
    Task<Result<IEnumerable<OrganizationClient>>> GetAllClients();
    Task<Result<IEnumerable<OrganizationClient>>> GetAllClientsByOrganizationId(Guid organizationId);
    Task<Result<OrganizationClient>> UpsertClient(OrganizationClient model);
    Task<Result<IEnumerable<OrganizationClient>>> UpsertMultipleClients(IEnumerable<OrganizationClient> modelList);
}