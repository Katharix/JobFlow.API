using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services
{
    public class OrganizationTypeService : IOrganizationTypeService
    {
        private readonly ILogger<OrganizationTypeService> logger;
        private readonly IUnitOfWork unitOfWork;
        public OrganizationTypeService(ILogger<OrganizationTypeService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
        }
        public Task DeleteMultipleOrganizationTypes(IEnumerable<OrganizationType> idList)
        {
            throw new NotImplementedException();
        }

        public Task DeleteOrganizationType(Guid organizationTypeId)
        {
            throw new NotImplementedException();
        }

        public Task<OrganizationType> GetTypeById(Guid organizationTypeId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OrganizationType>> GetTypes()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OrganizationType>> UpsertOrganizationList(IEnumerable<OrganizationType> modelList)
        {
            throw new NotImplementedException();
        }

        public Task<OrganizationType> UpsertOrganizationType(OrganizationType model)
        {
            throw new NotImplementedException();
        }
    }
}
