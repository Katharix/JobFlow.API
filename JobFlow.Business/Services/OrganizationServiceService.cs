using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationServiceService : IOrganizationServiceService
{
    private readonly ILogger<OrganizationServiceService> logger;
    private readonly IRepository<Domain.Models.OrganizationService> organizationService;
    private readonly IUnitOfWork unitOfWork;

    public OrganizationServiceService(ILogger<OrganizationServiceService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        organizationService = this.unitOfWork.RepositoryOf<Domain.Models.OrganizationService>();
    }

    public async Task<Result> DeleteOrganizationService(Guid serviceId)
    {
        if (serviceId == Guid.Empty) return Result.Failure(OrganizationServiceErrors.NullOrEmptyId);
        var organizationToDelete = await organizationService.Query().FirstOrDefaultAsync(ser => ser.Id == serviceId);
        if (organizationToDelete == null) return Result.Failure(OrganizationServiceErrors.NoServiceFound);
        var serviceName = organizationToDelete.ServiceName;
        organizationService.Remove(organizationToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"{serviceName} was deleted successfully.");
    }

    public async Task<Result<IEnumerable<Domain.Models.OrganizationService>>> GetAllOrganizationServices()
    {
        var orgServices = await organizationService.Query().ToListAsync();

        if (orgServices.Count == 0)
            return Result.Failure<IEnumerable<Domain.Models.OrganizationService>>(OrganizationServiceErrors
                .NoServiceFound);

        return Result.Success<IEnumerable<Domain.Models.OrganizationService>>(orgServices);
    }


    public async Task<Result<IEnumerable<Domain.Models.OrganizationService>>>
        GetAllOrganizationServicesByOrganizationId(Guid organizationId)
    {
        var organization = await unitOfWork.RepositoryOf<Organization>().Query()
            .FirstOrDefaultAsync(org => org.Id == organizationId);
        if (organization == null)
            return Result.Failure<IEnumerable<Domain.Models.OrganizationService>>(Error.NullValue);
        var orgServices = await organizationService.Query().Where(org => org.OrganizationId == organizationId)
            .ToListAsync();
        if (orgServices.Count == 0)
            return Result.Failure<IEnumerable<Domain.Models.OrganizationService>>(
                OrganizationServiceErrors.NoServiceFoundForOrganizationName(organization.OrganizationName ?? string.Empty));

        return Result.Success<IEnumerable<Domain.Models.OrganizationService>>(orgServices);
    }

    public async Task<Result<Domain.Models.OrganizationService>> GetOrganizationServiceByOrganizationId(
        Guid organizationId)
    {
        var organization = await unitOfWork.RepositoryOf<Organization>().Query()
            .FirstOrDefaultAsync(org => org.Id == organizationId);
        if (organization == null) return Result.Failure<Domain.Models.OrganizationService>(Error.NullValue);
        var orgService = await organizationService.Query()
            .FirstOrDefaultAsync(org => org.OrganizationId == organizationId);
        if (orgService == null)
            return Result.Failure<Domain.Models.OrganizationService>(
                OrganizationServiceErrors.NoServiceFoundForOrganizationName(organization.OrganizationName ?? string.Empty));

        return Result.Success<Domain.Models.OrganizationService>(orgService);
    }

    public async Task<Result<Domain.Models.OrganizationService>> GetOrganizationServiceByServiceName(string serviceName)
    {
        var orgService = await organizationService.Query().FirstOrDefaultAsync(org => org.ServiceName == serviceName);
        if (orgService == null)
            return Result.Failure<Domain.Models.OrganizationService>(OrganizationServiceErrors.NoServiceFound);

        return Result.Success<Domain.Models.OrganizationService>(orgService);
    }

    public async Task<Result<IEnumerable<Domain.Models.OrganizationService>>> UpsertMultipleOrganizationServices(
        IEnumerable<Domain.Models.OrganizationService> services)
    {
        if (services == null || !services.Any())
            return Result.Failure<IEnumerable<Domain.Models.OrganizationService>>(OrganizationServiceErrors
                .NoOrganizationServicesToUpsert);

        var servicesToInsert = new List<Domain.Models.OrganizationService>();
        var servicesToUpdate = new List<Domain.Models.OrganizationService>();

        foreach (var service in services)
            if (service.Id == Guid.Empty)
                servicesToInsert.Add(service);
            else
                servicesToUpdate.Add(service);

        if (servicesToInsert.Count > 0) organizationService.AddRange(servicesToInsert);

        if (servicesToUpdate.Count > 0) organizationService.UpdateRange(servicesToUpdate);

        await unitOfWork.SaveChangesAsync();
        return Result.Success<IEnumerable<Domain.Models.OrganizationService>>(services);
    }


    public async Task<Result<Domain.Models.OrganizationService>> UpsertOrganizationService(
        Domain.Models.OrganizationService service)
    {
        if (service == null) return Result.Failure<Domain.Models.OrganizationService>(Error.NullValue);

        if (service.Id == Guid.Empty)
            organizationService.Add(service);
        else
            organizationService.Update(service);

        await unitOfWork.SaveChangesAsync();
        return Result.Success<Domain.Models.OrganizationService>(service);
    }
}