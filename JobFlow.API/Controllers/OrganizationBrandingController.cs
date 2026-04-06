using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationBrandingController : ControllerBase
{
    private readonly IOrganizationBrandingService _brandingService;

    public OrganizationBrandingController(IOrganizationBrandingService brandingService)
    {
        _brandingService = brandingService;
    }

    [AllowAnonymous]
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