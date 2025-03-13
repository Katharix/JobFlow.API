using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IOrganizationServiceService
    {
        Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> GetAllOrganizationServices();
        Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> GetAllOrganizationServicesByOrganizationId(Guid organizationId);
        Task<Result<JobFlow.Domain.Models.OrganizationService>> GetOrganizationServiceByOrganizationId(Guid organizationId);
        Task<Result<JobFlow.Domain.Models.OrganizationService>> GetOrganizationServiceByServiceName(string serviceName);
        Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> UpsertMultipleOrganizationServices(IEnumerable<JobFlow.Domain.Models.OrganizationService> services);
        Task<Result<JobFlow.Domain.Models.OrganizationService>> UpsertOrganizationService(JobFlow.Domain.Models.OrganizationService service);
        Task<Result> DeleteOrganizationService(Guid serviceId);
    }
}
