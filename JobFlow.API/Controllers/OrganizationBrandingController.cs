using JobFlow.API.Mappings;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Extensions;
using JobFlow.Domain;
using Microsoft.AspNetCore.Mvc;
using JobFlow.API.Models;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationBrandingController : ControllerBase
    {
        private readonly IOrganizationBrandingService _brandingService;

        public OrganizationBrandingController(IOrganizationBrandingService brandingService)
        {
            _brandingService = brandingService;
        }

        [HttpGet("{organizationId}")]
        public async Task<IResult> GetBranding(Guid organizationId)
        {
            var result = await _brandingService.GetByOrganizationIdAsync(organizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost]
        public async Task<IResult> CreateOrUpdateBranding([FromBody] BrandingDto dto)
        {
            var entity = OrganizationBrandingMapper.ToEntity(dto);
            var result = await _brandingService.CreateOrUpdateAsync(entity);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }
    }
}
