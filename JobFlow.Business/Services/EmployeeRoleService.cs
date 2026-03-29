using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class EmployeeRoleService : IEmployeeRoleService
{
    private readonly IRepository<EmployeeRole> employeeRoles;
    private readonly IRepository<Employee> employees;
    private readonly ILogger<EmployeeRoleService> logger;
    private readonly IUnitOfWork unitOfWork;

    public EmployeeRoleService(ILogger<EmployeeRoleService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        employeeRoles = unitOfWork.RepositoryOf<EmployeeRole>();
        employees = unitOfWork.RepositoryOf<Employee>();
    }

    // 🔹 Get all roles for an organization
    public async Task<Result<IEnumerable<EmployeeRole>>> GetRolesByOrganizationAsync(Guid organizationId)
    {
        var roles = await employeeRoles.Query()
            .Where(r => r.OrganizationId == organizationId)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return Result<IEnumerable<EmployeeRole>>.Success(roles.AsEnumerable());
    }

    // 🔹 Get a single role by Id
    public async Task<Result<EmployeeRole>> GetByIdAsync(Guid id)
    {
        var role = await employeeRoles.Query().FirstOrDefaultAsync(r => r.Id == id);
        if (role == null)
            return Result.Failure<EmployeeRole>(EmployeeRoleErrors.EmployeeRoleNotFound);

        return Result<EmployeeRole>.Success(role);
    }

    // 🔹 Create or update a role
    public async Task<Result<EmployeeRole>> UpsertAsync(EmployeeRole model)
    {
        var exists = await employeeRoles.Query().AnyAsync(r => r.Id == model.Id);

        if (exists)
            employeeRoles.Update(model);
        else
            await employeeRoles.AddAsync(model);

        await unitOfWork.SaveChangesAsync();
        return Result<EmployeeRole>.Success(model);
    }

    public async Task<Result<IEnumerable<EmployeeRoleUsageDto>>> GetRoleUsageByOrganizationAsync(Guid organizationId)
    {
        var roles = await employeeRoles.Query()
            .Where(role => role.OrganizationId == organizationId)
            .Select(role => role.Id)
            .ToListAsync();

        if (roles.Count == 0)
        {
            return Result<IEnumerable<EmployeeRoleUsageDto>>.Success(Enumerable.Empty<EmployeeRoleUsageDto>());
        }

        var counts = await employees.Query()
            .Where(employee => employee.OrganizationId == organizationId)
            .GroupBy(employee => employee.RoleId)
            .Select(group => new EmployeeRoleUsageDto
            {
                RoleId = group.Key,
                EmployeeCount = group.Count()
            })
            .ToListAsync();

        var usage = roles.Select(roleId =>
            counts.FirstOrDefault(count => count.RoleId == roleId)
            ?? new EmployeeRoleUsageDto { RoleId = roleId, EmployeeCount = 0 })
            .ToList();

        return Result<IEnumerable<EmployeeRoleUsageDto>>.Success(usage.AsEnumerable());
    }

    // 🔹 Delete a role
    public async Task<Result> DeleteAsync(Guid id)
    {
        var role = await employeeRoles.Query().FirstOrDefaultAsync(r => r.Id == id);
        if (role == null)
            return Result.Failure(EmployeeRoleErrors.EmployeeRoleNotFound);

        employeeRoles.Remove(role);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}