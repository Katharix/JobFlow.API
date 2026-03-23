using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/employeerolepresets")]
public class EmployeeRolePresetsController : ControllerBase
{
    private readonly IEmployeeRolePresetService presetService;
    private readonly IOrganizationService organizationService;

    public EmployeeRolePresetsController(
        IEmployeeRolePresetService presetService,
        IOrganizationService organizationService)
    {
        this.presetService = presetService;
        this.organizationService = organizationService;
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgResult = await organizationService.GetOrganizationDtoById(organizationId);
        var industryKey = orgResult.IsSuccess ? orgResult.Value.IndustryKey : null;

        var result = await presetService.GetAvailablePresetsAsync(organizationId, industryKey);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapPresetToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await presetService.GetByIdAsync(organizationId, id);
        return result.IsSuccess ? Ok(MapPresetToDto(result.Value)) : BadRequest(result.Error);
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpPost]
    public async Task<IResult> Create(EmployeeRolePresetDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await presetService.CreateOrgPresetAsync(organizationId, model);
        return result.IsSuccess ? Results.Ok(MapPresetToDto(result.Value)) : result.ToProblemDetails();
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(Guid id, EmployeeRolePresetDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await presetService.UpdateOrgPresetAsync(organizationId, id, model);
        return result.IsSuccess ? Results.Ok(MapPresetToDto(result.Value)) : result.ToProblemDetails();
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await presetService.DeleteOrgPresetAsync(organizationId, id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpPost("{id:guid}/apply")]
    public async Task<IResult> ApplyPreset(Guid id, [FromQuery] bool overwriteExisting = true)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await presetService.ApplyPresetAsync(organizationId, id, overwriteExisting);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    private static EmployeeRolePresetDto MapPresetToDto(JobFlow.Domain.Models.EmployeeRolePreset preset)
    {
        return new EmployeeRolePresetDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            IndustryKey = preset.IndustryKey,
            IsSystem = preset.IsSystem,
            OrganizationId = preset.OrganizationId,
            Items = preset.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new EmployeeRolePresetItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    SortOrder = item.SortOrder
                })
                .ToList()
        };
    }
}
