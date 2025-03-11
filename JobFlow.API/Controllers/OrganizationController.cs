using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [Route("api/organizations/")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private IOrganizationService _organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }
        [HttpGet, Route("all")]
        public async Task<IResult> GetAllOrganizations()
        {
            var result = await _organizationService.GetAllOrganizations();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }
    }
}
