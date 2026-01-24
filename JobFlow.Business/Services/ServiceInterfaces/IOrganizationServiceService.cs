namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IOrganizationServiceService
{
    Task<Result<IEnumerable<Domain.Models.OrganizationService>>> GetAllOrganizationServices();

    Task<Result<IEnumerable<Domain.Models.OrganizationService>>> GetAllOrganizationServicesByOrganizationId(
        Guid organizationId);

    Task<Result<Domain.Models.OrganizationService>> GetOrganizationServiceByOrganizationId(Guid organizationId);
    Task<Result<Domain.Models.OrganizationService>> GetOrganizationServiceByServiceName(string serviceName);

    Task<Result<IEnumerable<Domain.Models.OrganizationService>>> UpsertMultipleOrganizationServices(
        IEnumerable<Domain.Models.OrganizationService> services);

    Task<Result<Domain.Models.OrganizationService>>
        UpsertOrganizationService(Domain.Models.OrganizationService service);

    Task<Result> DeleteOrganizationService(Guid serviceId);
}