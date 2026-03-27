using JobFlow.Domain.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationService
{
    Task<Result<Organization>> GetOrganiztionById(Guid OrgId);
    Task<Result<OrganizationDto>> GetOrganizationDtoById(Guid orgId);
    Task<Result<IEnumerable<Organization>>> GetAllOrganizations();
    Task<Result<Organization>> UpsertOrganization(Organization model);
    Task<Result<Organization>> UpdateIndustryAsync(Guid organizationId, string? industryKey);
    Task MarkStripeConnectedAsync(string stripeAccountId);
    Task<Result<Organization>> GetBySquareMerchantIdAsync(string squareMerchantId);
    Task MarkSquareDisconnectedAsync(string squareMerchantId);
    Task<Result> DeleteOrganization(Guid organizationId);
}