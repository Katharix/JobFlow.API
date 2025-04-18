using JobFlow.Business.Extensions;
using JobFlow.Business.ExternalServices.Twilio;
using JobFlow.Business.ExternalServices.Twilio.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [Route("api/organization/types/")]
    [ApiController]
    public class OrganizationTypeController : ControllerBase
    {
        private readonly IOrganizationTypeService organizationTypeService;
        private readonly ITwilioService _twilioService;

        public OrganizationTypeController(IOrganizationTypeService organizationTypeService, ITwilioService twilioService)
        {
            this.organizationTypeService = organizationTypeService;
            this._twilioService = twilioService;
        }

        [HttpGet, Route("all")]
        public async Task<IResult> GetAllOrganizationTypes()
        { 
            var result = await this.organizationTypeService.GetTypes();
            var twilio = new TwilioModel()
            {
                Message = " This is a test!",
                RecipientPhoneNumber = "+15406429153"
            };
            await  this._twilioService.SendTextMessage(twilio);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpGet, Route("id")]
        public async Task<IResult> GetTypeById(Guid organizationTypeId)
        {
            var result = await this.organizationTypeService.GetTypeById(organizationTypeId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("upsert")]
        public async Task<IResult> UpsertOrganization(OrganizationType model)
        {
            var result = await this.organizationTypeService.UpsertOrganizationType(model);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("upsert/list")]
        public async Task<IResult> UpsertOrganizations(IEnumerable<OrganizationType> modelList)
        {
            var result = await this.organizationTypeService.UpsertOrganizationList(modelList);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpDelete, Route("delete")]
        public async Task<IResult> DeleteOrganizationType(Guid organizationTypeId)
        {
            var result = await this.organizationTypeService.DeleteOrganizationType(organizationTypeId);
            return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
        }

        [HttpDelete, Route("delete/list")]
        public async Task<IResult> DeleteMultipleOrganizationTypes(IEnumerable<Guid> idList)
        {
            var result = await this.organizationTypeService.DeleteMultipleOrganizationTypes(idList);
            return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
        }
    }
}
