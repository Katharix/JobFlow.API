using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobFlow.Business.Mappers;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Employee> _employeeRepo;
        private readonly IRepository<Organization> _orgRepo;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            ILogger<EmployeeService> logger)
        {
            _unitOfWork = unitOfWork;
            _employeeRepo = unitOfWork.RepositoryOf<Employee>();
            _orgRepo = unitOfWork.RepositoryOf<Organization>();
            _logger = logger;
        }

        public async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request)
        {
            var org = await _orgRepo.Query().FirstOrDefaultAsync(e => e.Id == request.OrganizationId);
            if (org == null)
                return Result.Failure<EmployeeDto>(EmployeeErrors.InvalidOrganization);

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                RoleId = request.RoleId,
                IsActive = true
            };

            await _employeeRepo.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            return employee.ToDto();
        }

        public async Task<Result<EmployeeDto>> UpdateAsync(Guid employeeId, UpdateEmployeeRequest request)
        {
            var employee = await _employeeRepo.Query().FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
                return Result.Failure<EmployeeDto>(EmployeeErrors.NotFound);

            employee.FirstName = request.FirstName;
            employee.LastName = request.LastName;
            employee.Email = request.Email;
            employee.PhoneNumber = request.PhoneNumber;
            employee.RoleId = request.RoleId;
            employee.IsActive = request.IsActive;

            _employeeRepo.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            return employee.ToDto();
        }

        public async Task<Result> DeleteAsync(Guid employeeId)
        {
            var employee = await _employeeRepo.Query().FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
                return Result.Failure(EmployeeErrors.NotFound);

            _employeeRepo.Remove(employee);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<EmployeeDto>> GetByIdAsync(Guid employeeId)
        {
            var employee = await _employeeRepo.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            return employee is null
                ? Result.Failure<EmployeeDto>(EmployeeErrors.NotFound)
                : employee.ToDto();
        }

        public async Task<Result<List<EmployeeDto>>> GetByOrganizationIdAsync(Guid organizationId)
        {
            var employees = await _employeeRepo.Query()
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId)
                .ToListAsync();

            return employees.Select(e => e.ToDto()).ToList();
        }
    }
}
