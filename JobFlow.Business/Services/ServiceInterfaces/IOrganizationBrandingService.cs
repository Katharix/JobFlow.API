using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrganizationBrandingService
    {
        Task<Result<OrganizationBranding>> GetByOrganizationIdAsync(Guid organizationId);
        Task<Result<OrganizationBranding>> CreateOrUpdateAsync(OrganizationBranding model);
    }
}
