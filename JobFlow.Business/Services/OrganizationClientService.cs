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
    public class OrganizationClientService : IOrganizationClientService
    {
        private readonly ILogger<OrganizationClientService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<OrganizationClient> organizationClient;

        public OrganizationClientService(ILogger<OrganizationClientService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.organizationClient = this.unitOfWork.RepositoryOf<OrganizationClient>();
        }

        public async Task<Result> DeleteClient(Guid clientId)
        {
            var clientToDelete = await this.organizationClient.Query().FirstOrDefaultAsync(client => client.Id == clientId);
            if (clientToDelete == null)
            {
                return Result.Failure(OrganizationClientErrors.NoClientFound);
            }
            var clientName = clientToDelete.ClientFullName();
            organizationClient.Remove(clientToDelete);
            await this.unitOfWork.SaveChangesAsync();

            return Result.Success($"{clientName} was successfully removed.");
        }

        public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClients()
        {
            var clients = await this.organizationClient.Query().ToListAsync();
            if (!clients.Any())
            {
                return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.NoClientsToShow);
            }

            return Result.Success<IEnumerable<OrganizationClient>>(clients);
        }

        public async Task<Result<IEnumerable<OrganizationClient>>> GetAllClientsByOrganizationId(Guid organizationId)
        {
            var clients = await this.organizationClient.Query().Where(client => client.OrganizationId == organizationId).ToListAsync();
            if (!clients.Any())
            {
                return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.NoClientFound);
            }

            return Result.Success<IEnumerable<OrganizationClient>>(clients);
        }

        public async Task<Result<OrganizationClient>> GetClientById(Guid clientId)
        {
            var client = await this.organizationClient.Query().FirstOrDefaultAsync(cli => cli.Id == clientId);
            if (client == null)
            {
                return Result.Failure<OrganizationClient>(OrganizationClientErrors.NoClientFound);
            }

            return Result.Success<OrganizationClient>(client);
        }

        public async Task<Result> UpsertClient(OrganizationClient model)
        {
            if (model.Id == Guid.Empty)
            {
                this.organizationClient.Add(model);
            }
            else
            {
                this.organizationClient.Update(model);
            }

            await this.unitOfWork.SaveChangesAsync();
            return Result.Success<OrganizationClient>(model);
        }

        public async Task<Result<IEnumerable<OrganizationClient>>> UpsertMultipleClients(IEnumerable<OrganizationClient> modelList)
        {
            var modelsToInsert = modelList.Where(client => client.Id == Guid.Empty);
            var modelsToUpdate = modelList.Where(client => client.Id != Guid.Empty);

            if (!modelList.Any())
            {
                return Result.Failure<IEnumerable<OrganizationClient>>(OrganizationClientErrors.FailedToCreateClient);
            }

            if (modelsToInsert.Any())
            {
                this.organizationClient.AddRange(modelsToInsert);
            }
            if (modelsToUpdate.Any())
            {
                this.organizationClient.UpdateRange(modelsToUpdate);
            }

            await this.unitOfWork.SaveChangesAsync();
            return Result.Success<IEnumerable<OrganizationClient>>(modelList);
        }
    }
}
