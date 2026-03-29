using JobFlow.Domain.Models;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationClientService
{
    Task<Result> DeleteClient(Guid clientId);
    Task<Result> DeleteClient(Guid clientId, Guid organizationId);
    Task<Result<OrganizationClient>> GetClientById(Guid clientId);
    Task<Result<IEnumerable<OrganizationClient>>> GetAllClients();
    Task<Result<IEnumerable<OrganizationClient>>> GetAllClientsByOrganizationId(Guid organizationId);
    Task<Result<OrganizationClient>> GetOrganizationClientByEmailAsync(string emailAddress);
    Task<Result<IReadOnlyList<OrganizationClient>>> GetOrganizationClientsByEmailAsync(string emailAddress);
    Task<Result<OrganizationClient>> UpsertClient(OrganizationClient model);
    Task<Result<IEnumerable<OrganizationClient>>> UpsertMultipleClients(IEnumerable<OrganizationClient> modelList);
    Task<Result> RestoreClient(Guid clientId, Guid organizationId);
}