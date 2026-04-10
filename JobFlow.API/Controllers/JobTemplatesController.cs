using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/jobtemplates")]
public class JobTemplatesController : ControllerBase
{
    private readonly IJobTemplateService _templateService;
    private readonly IOrganizationService _organizationService;

    public JobTemplatesController(
        IJobTemplateService templateService,
        IOrganizationService organizationService)
    {
        _templateService = templateService;
        _organizationService = organizationService;
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgResult = await _organizationService.GetOrganizationDtoById(organizationId);
        var organizationTypeId = orgResult.IsSuccess ? orgResult.Value.OrganizationTypeId : (Guid?)null;

        var result = await _templateService.GetAvailableTemplatesAsync(organizationId, organizationTypeId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        var dtos = result.Value.Select(MapTemplateToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _templateService.GetByIdAsync(organizationId, id);
        return result.IsSuccess ? Ok(MapTemplateToDto(result.Value)) : BadRequest(result.Error);
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpPost]
    public async Task<IResult> Create(JobTemplateDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _templateService.CreateOrgTemplateAsync(organizationId, model);
        return result.IsSuccess ? Results.Ok(MapTemplateToDto(result.Value)) : result.ToProblemDetails();
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(Guid id, JobTemplateDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _templateService.UpdateOrgTemplateAsync(organizationId, id, model);
        return result.IsSuccess ? Results.Ok(MapTemplateToDto(result.Value)) : result.ToProblemDetails();
    }

    [Authorize(Policy = "OrganizationAdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _templateService.DeleteOrgTemplateAsync(organizationId, id);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    private static JobTemplateDto MapTemplateToDto(JobFlow.Domain.Models.JobTemplate template)
    {
        return new JobTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            OrganizationTypeId = template.OrganizationTypeId,
            OrganizationTypeName = template.OrganizationType?.TypeName,
            DefaultInvoicingWorkflow = template.DefaultInvoicingWorkflow,
            IsSystem = template.IsSystem,
            OrganizationId = template.OrganizationId,
            Items = template.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new JobTemplateItemDto
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
