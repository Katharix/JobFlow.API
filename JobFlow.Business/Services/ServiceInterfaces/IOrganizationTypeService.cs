using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    interface IOrganizationTypeService
    {
        Task<IEnumerable<OrganizationType>> GetTypes();
        Task<OrganizationType> GetTypeById(Guid organizationTypeId);
        Task<OrganizationType> UpsertOrganizationType(OrganizationType model);
        Task<IEnumerable<OrganizationType>> UpsertOrganizationList(IEnumerable<OrganizationType> modelList);
        Task DeleteOrganizationType(Guid organizationTypeId);
        Task DeleteMultipleOrganizationTypes(IEnumerable<OrganizationType> idList);
    }
}
