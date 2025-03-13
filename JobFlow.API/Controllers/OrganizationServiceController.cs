using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobFlow.API.Controllers
{
    [Route("api/organizations/services")]
    [ApiController]
    public class OrganizationServiceController : ControllerBase
    {
        private readonly IOrganizationServiceService organizationServiceService;

        public OrganizationServiceController(IOrganizationServiceService organizationServiceService)
        {
            this.organizationServiceService = organizationServiceService;
        }

        /// <summary>
        /// Gets all organization services.
        /// </summary>
        [HttpGet, Route("all")]
        public async Task<IResult> GetAllOrganizationServices()
        {
            var result = await organizationServiceService.GetAllOrganizationServices();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Gets all services by Organization ID.
        /// </summary>
        [HttpGet, Route("organization/{organizationId}")]
        public async Task<IResult> GetAllOrganizationServicesByOrganizationId([FromRoute] Guid organizationId)
        {
            var result = await organizationServiceService.GetAllOrganizationServicesByOrganizationId(organizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Gets a single organization service by Organization ID.
        /// </summary>
        [HttpGet, Route("organization/service/{organizationId}")]
        public async Task<IResult> GetOrganizationServiceByOrganizationId([FromRoute] Guid organizationId)
        {
            var result = await organizationServiceService.GetOrganizationServiceByOrganizationId(organizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Gets a single organization service by its name.
        /// </summary>
        [HttpGet, Route("name/{serviceName}")]
        public async Task<IResult> GetOrganizationServiceByServiceName([FromRoute] string serviceName)
        {
            var result = await organizationServiceService.GetOrganizationServiceByServiceName(serviceName);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Upserts multiple organization services.
        /// </summary>
        [HttpPost, Route("upsert-multiple")]
        public async Task<IResult> UpsertMultipleOrganizationServices([FromBody] IEnumerable<OrganizationService> services)
        {
            var result = await organizationServiceService.UpsertMultipleOrganizationServices(services);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Upserts a single organization service.
        /// </summary>
        [HttpPost, Route("upsert")]
        public async Task<IResult> UpsertOrganizationService([FromBody] OrganizationService service)
        {
            var result = await organizationServiceService.UpsertOrganizationService(service);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        /// <summary>
        /// Deletes an organization service.
        /// </summary>
        [HttpDelete, Route("delete/{serviceId}")]
        public async Task<IResult> DeleteOrganizationService([FromRoute] Guid serviceId)
        {
            var result = await organizationServiceService.DeleteOrganizationService(serviceId);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        }
    }
}
