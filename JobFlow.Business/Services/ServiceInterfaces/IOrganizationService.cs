using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
