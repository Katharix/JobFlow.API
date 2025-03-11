using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [Route("api/organiztion/clients/")]
    [ApiController]
    public class OrganizationClientController : ControllerBase
    {
        private readonly IOrganizationClientService organizationClientService;

        public OrganizationClientController(IOrganizationClientService organizationClientService)
        {
            this.organizationClientService = organizationClientService;
        }

        [HttpGet, Route("all")]
        public async Task<IResult> GetAllClients()
        {
            var result = await this.organizationClientService.GetAllClients();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpGet, Route("all/organizationId")]
        public async Task<IResult> GetAllClientsByOrganizationId(Guid organizationId)
        {
            var result = await this.organizationClientService.GetAllClientsByOrganizationId(organizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpDelete, Route("delete")]
        public async Task<IResult> DeleteClient(Guid clientId)
        {
            var result = await this.organizationClientService.DeleteClient(clientId);
            return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
        }

        [HttpPost, Route("upsert")]
        public async Task<IResult> UpsertClient(OrganizationClient model)
        {
            var result = await this.organizationClientService.UpsertClient(model);
            return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
        }

        [HttpPost, Route("upsert/multi")]
        public async Task<IResult> UpsertMultipleClients(IEnumerable<OrganizationClient> modelList)
        {
            var result = await this.organizationClientService.UpsertMultipleClients(modelList);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }
    }
}
