using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IEmployeeRoleService
    {
        Task<Result<IEnumerable<EmployeeRole>>> GetRolesByOrganizationAsync(Guid organizationId);
        Task<Result<EmployeeRole>> GetByIdAsync(Guid id);
        Task<Result<EmployeeRole>> UpsertAsync(EmployeeRole model);
        Task<Result> DeleteAsync(Guid id);
    }
}
