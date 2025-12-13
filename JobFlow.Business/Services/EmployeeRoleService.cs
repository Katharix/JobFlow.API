using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobFlow.Business.DI;

namespace JobFlow.Business.Services
{
    [ScopedService]
    public class EmployeeRoleService : IEmployeeRoleService
    {
        private readonly ILogger<EmployeeRoleService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<EmployeeRole> employeeRoles;

        public EmployeeRoleService(ILogger<EmployeeRoleService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.employeeRoles = unitOfWork.RepositoryOf<EmployeeRole>();
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
}
