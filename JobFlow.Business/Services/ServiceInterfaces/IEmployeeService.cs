using JobFlow.Business.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IEmployeeService
    {
        Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request);
        Task<Result<EmployeeDto>> UpdateAsync(Guid employeeId, UpdateEmployeeRequest request);
        Task<Result> DeleteAsync(Guid employeeId);
        Task<Result<EmployeeDto>> GetByIdAsync(Guid employeeId);
        Task<Result<List<EmployeeDto>>> GetByOrganizationIdAsync(Guid organizationId);
    }
}
