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
    public class OrganizationService : IOrganizationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private ILogger<OrganizationService> _logger;
        private IQueryable<Organization> _organizations;

        public OrganizationService(IUnitOfWork unitOfWork, ILogger<OrganizationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _organizations = _unitOfWork.RepositoryOf<Organization>().Query().Include(e => e.OrganizationType);
        }
        public async Task<Result> DeleteOrganization(Guid organizationId)
        {
            var organizationToDelete = _organizations.FirstOrDefault(org => org.Id == organizationId);
            if (organizationToDelete == null)
            {
                return Result.Failure(OrganizationErrors.OrganizationNotFound);
            }
            _unitOfWork.RepositoryOf<Organization>().Remove(organizationToDelete);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success($"Organization: {organizationToDelete.OrganizationName} has been removed successfully.");
        }

        public async Task<Result<IEnumerable<Organization>>> GetAllOrganizations()
        {
            var organizations = _organizations.AsEnumerable();

            if (!organizations.Any())
            {
                return Result.Failure<IEnumerable<Organization>>(OrganizationErrors.OrganizationNotFound);
            }
            return Result.Success(organizations);
        }

        public async Task<Result<Organization>> GetOrganiztionById(Guid orgId)
        {
            var organization = _organizations.FirstOrDefault(org => org.Id == orgId);

            if (organization == null)
            {
                return Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound);
            }
            return Result.Success(organization);
        }

        public async Task<Result<Organization>> UpsertOrganization(Organization model)
        {
            if (model.Id == Guid.Empty)
            {
                _unitOfWork.RepositoryOf<Organization>().Add(model);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var organization = _organizations.FirstOrDefault(org => org.Id == model.Id);
                if (organization == null)
                {
                    return Result.Failure<Organization>(OrganizationErrors.OrganizationNotFound);
                }
                _unitOfWork.RepositoryOf<Organization>().Update(model);
                await _unitOfWork.SaveChangesAsync();
            }
            return Result.Success(model);
        }
    }
}
