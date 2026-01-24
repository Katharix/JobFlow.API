using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Onboarding;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationClientService : IOrganizationClientService
{
    private readonly ILogger<OrganizationClientService> logger;
    private readonly IRepository<OrganizationClient> organizationClient;
    private readonly IOnboardingService onboardingService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper _mapper;

    public OrganizationClientService(ILogger<OrganizationClientService> logger, IUnitOfWork unitOfWork, IOnboardingService onboardingService, IMapper mapper)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
        this.onboardingService = onboardingService;
        organizationClient = this.unitOfWork.RepositoryOf<OrganizationClient>();
        _mapper = mapper;
    }

    public async Task<Result> DeleteClient(Guid clientId)
    {
        var clientToDelete = await organizationClient.Query().FirstOrDefaultAsync(client => client.Id == clientId);
        if (clientToDelete == null) return Result.Failure(OrganizationClientErrors.NoClientFound);
        var clientName = clientToDelete.ClientFullName();
        organizationClient.Remove(clientToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success($"{clientName} was successfully removed.");
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClients()
    {
        var clients = await organizationClient.Query().ToListAsync();
        if (!clients.Any())
            return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.NoClientsToShow);

        return Result.Success<IEnumerable<OrganizationClient>>(clients);
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClientsByOrganizationId(Guid organizationId)
    {
        var clients = await organizationClient.Query().Where(client => client.OrganizationId == organizationId)
            .ToListAsync();
        if (!clients.Any())
            return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.NoClientFound);

        return Result.Success<IEnumerable<OrganizationClient>>(clients);
    }

    public async Task<Result<OrganizationClient>> GetClientById(Guid clientId)
    {
        var client = await organizationClient.Query().FirstOrDefaultAsync(cli => cli.Id == clientId);
        if (client == null) return Result.Failure<OrganizationClient>(OrganizationClientErrors.NoClientFound);

        return Result.Success<OrganizationClient>(client);
    }

    public async Task<Result> UpsertClient(OrganizationClient model)
    {
        var exists = await organizationClient.Query()
            .AnyAsync(c => c.Id == model.Id);

        if (exists)
        {
            organizationClient.Update(model);
        }
        else
        {
            await organizationClient.AddAsync(model);
        }

        await unitOfWork.SaveChangesAsync();
        
        if (!exists)
        {
            await onboardingService.MarkStepCompleteAsync(
                model.OrganizationId,
                OnboardingStepKeys.CreateCustomer
            );
        }
        return Result.Success(model);
    }

    public async Task<Result<IEnumerable<OrganizationClient>>> UpsertMultipleClients(
        IEnumerable<OrganizationClient> modelList)
    {
        var modelsToInsert = modelList.Where(client => client.Id == Guid.Empty);
        var modelsToUpdate = modelList.Where(client => client.Id != Guid.Empty);

        if (!modelList.Any())
            return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.FailedToCreateClient);

        if (modelsToInsert.Any()) organizationClient.AddRange(modelsToInsert);
        if (modelsToUpdate.Any()) organizationClient.UpdateRange(modelsToUpdate);

        await unitOfWork.SaveChangesAsync();
        return Result.Success<IEnumerable<OrganizationClient>>(modelList);
    }
}