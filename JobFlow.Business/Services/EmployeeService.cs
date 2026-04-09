using JobFlow.Business.DI;
using JobFlow.Business.Mappers;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepo;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IRepository<Organization> _orgRepo;
    private readonly IUnitOfWork _unitOfWork;

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
        var org = await _orgRepo.Query().Include(e => e.EmployeeRoles)
            .FirstOrDefaultAsync(e => e.Id == request.OrganizationId);
        if (org == null)
            return Result.Failure<EmployeeDto>(EmployeeErrors.InvalidOrganization);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId.GetValueOrDefault(),
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
        var employee = await _employeeRepo.Query().Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == employeeId);
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
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        return employee is null
            ? Result.Failure<EmployeeDto>(EmployeeErrors.NotFound)
            : employee.ToDto();
    }

    public async Task<Result<List<EmployeeDto>>> GetByOrganizationIdAsync(Guid organizationId)
    {
        var employees = await _employeeRepo.Query()
            .AsNoTracking()
            .Include(e => e.Role)
            .Where(e => e.OrganizationId == organizationId)
            .ToListAsync();

        return employees.Select(e => e.ToDto()).ToList();
    }

    public async Task<Result<bool>> EmployeeExistByEmailAsync(Guid organizationId, string email)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<bool>(EmployeeErrors.InvalidOrganization);

        if (email == null)
            return Result.Failure<bool>(EmployeeErrors.NotFound);

        var employeeExist =
            await _employeeRepo.ExistsAsync(e => e.Email == email.Trim() && e.OrganizationId == organizationId);
        return Result.Success(employeeExist);
    }

    public async Task<Result<List<EmployeeDto>>> BulkCreateAsync(Guid organizationId, List<CreateEmployeeRequest> requests)
    {
        if (requests.Count == 0)
            return Result.Failure<List<EmployeeDto>>(EmployeeErrors.InvalidRequest);

        var org = await _orgRepo.Query().Include(e => e.EmployeeRoles)
            .FirstOrDefaultAsync(e => e.Id == organizationId);
        if (org == null)
            return Result.Failure<List<EmployeeDto>>(EmployeeErrors.InvalidOrganization);

        var created = new List<Employee>();
        foreach (var request in requests)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                RoleId = request.RoleId,
                IsActive = true
            };

            await _employeeRepo.AddAsync(employee);
            created.Add(employee);
        }

        await _unitOfWork.SaveChangesAsync();
        return created.Select(e => e.ToDto()).ToList();
    }
}