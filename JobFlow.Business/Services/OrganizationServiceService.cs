using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    public class OrganizationServiceService : IOrganizationServiceService
    {
        private readonly ILogger<OrganizationServiceService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<JobFlow.Domain.Models.OrganizationService> organizationService;
        
        public OrganizationServiceService(ILogger<OrganizationServiceService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.organizationService = this.unitOfWork.RepositoryOf<JobFlow.Domain.Models.OrganizationService>();
        }
        public async Task<Result> DeleteOrganizationService(Guid serviceId)
        {
            var organizationToDelete = await this.organizationService.Query().FirstOrDefaultAsync(ser => ser.Id == serviceId);
            if (organizationToDelete == null)
            {
                return Result.Failure(OrganizationServiceErrors.NoServiceFound);
            }
            var serviceName = organizationToDelete.ServiceName;
            this.organizationService.Remove(organizationToDelete);
            await this.unitOfWork.SaveChangesAsync();

            return Result.Success($"{serviceName} was deleted successfully.");
        }

        public async Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> GetAllOrganizationServices()
        {
            var orgServices = await this.organizationService.Query().ToListAsync();

            if (orgServices.Count == 0)
            {
                return Result.Failure<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(OrganizationServiceErrors.NoServiceFound);
            }

            return Result.Success<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(orgServices);
        }


        public async Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> GetAllOrganizationServicesByOrganizationId(Guid organizationId)
        {
            var organization = await this.unitOfWork.RepositoryOf<Organization>().Query().FirstOrDefaultAsync(org => org.Id == organizationId);
            if (organization == null)
            {
                return Result.Failure<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(Error.NullValue);
            }
            var orgServices = await this.organizationService.Query().Where(org => org.OrganizationId == organizationId).ToListAsync();
            if (orgServices.Count == 0)
            {
                return Result.Failure<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(OrganizationServiceErrors.NoServiceFoundForOrganizationName(organization.OrganizationName));
            }

            return Result.Success<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(orgServices);
        }

        public async Task<Result<JobFlow.Domain.Models.OrganizationService>> GetOrganizationServiceByOrganizationId(Guid organizationId)
        {
            var organization = await this.unitOfWork.RepositoryOf<Organization>().Query().FirstOrDefaultAsync(org => org.Id == organizationId);
            if (organization == null)
            {
                return Result.Failure<JobFlow.Domain.Models.OrganizationService>(Error.NullValue);
            }
            var orgService = await this.organizationService.Query().FirstOrDefaultAsync(org => org.OrganizationId == organizationId);
            if (orgService == null)
            {
                return Result.Failure<JobFlow.Domain.Models.OrganizationService>(OrganizationServiceErrors.NoServiceFoundForOrganizationName(organization.OrganizationName));
            }

            return Result.Success<JobFlow.Domain.Models.OrganizationService>(orgService);
        }

        public async Task<Result<JobFlow.Domain.Models.OrganizationService>> GetOrganizationServiceByServiceName(string serviceName)
        {
            var orgService = await this.organizationService.Query().FirstOrDefaultAsync(org => org.ServiceName == serviceName);
            if (orgService == null)
            {
                return Result.Failure<JobFlow.Domain.Models.OrganizationService>(OrganizationServiceErrors.NoServiceFound);
            }

            return Result.Success<JobFlow.Domain.Models.OrganizationService>(orgService);
        }

        public async Task<Result<IEnumerable<JobFlow.Domain.Models.OrganizationService>>> UpsertMultipleOrganizationServices(IEnumerable<JobFlow.Domain.Models.OrganizationService> services)
        {
            if (services == null || !services.Any())
            {
                return Result.Failure<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(OrganizationServiceErrors.NoOrganizationServicesToUpsert);
            }

            var servicesToInsert = new List<JobFlow.Domain.Models.OrganizationService>();
            var servicesToUpdate = new List<JobFlow.Domain.Models.OrganizationService>();

            foreach (var service in services)
            {
                if (service.Id == Guid.Empty)
                    servicesToInsert.Add(service);
                else
                    servicesToUpdate.Add(service);
            }

            if (servicesToInsert.Count > 0)
            {
                organizationService.AddRange(servicesToInsert);
            }

            if (servicesToUpdate.Count > 0)
            {
                organizationService.UpdateRange(servicesToUpdate);
            }

            await unitOfWork.SaveChangesAsync();
            return Result.Success<IEnumerable<JobFlow.Domain.Models.OrganizationService>>(services);
        }


        public async Task<Result<JobFlow.Domain.Models.OrganizationService>> UpsertOrganizationService(JobFlow.Domain.Models.OrganizationService service)
        {
            if (service == null)
            {
                return Result.Failure<JobFlow.Domain.Models.OrganizationService>(Error.NullValue);
            }

            if (service.Id == Guid.Empty)
            {
                organizationService.Add(service);
            }
            else
            {
                organizationService.Update(service);
            }

            await unitOfWork.SaveChangesAsync();
            return Result.Success< JobFlow.Domain.Models.OrganizationService> (service);
        }

    }
}
