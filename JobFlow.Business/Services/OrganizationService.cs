using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    public class OrganizationService : IOrganizationService
    {
        public Task<Result> DeleteOrganization(Guid organizationId)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<Organization>>> GetAllOrganizations()
        {
            throw new NotImplementedException();
        }

        public Task<Result<Organization>> GetOrganiztionById(Guid OrgId)
        {
            throw new NotImplementedException();
        }

        public Task<Result<Organization>> UpsertOrganizatiom(Organization model)
        {
            throw new NotImplementedException();
        }
    }
}
