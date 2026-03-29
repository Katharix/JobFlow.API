using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mapster;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeRolesController : ControllerBase
{
    private readonly IEmployeeRoleService employeeRoleService;
    private readonly ILogger<EmployeeRolesController> logger;

    public EmployeeRolesController(IEmployeeRoleService employeeRoleService, ILogger<EmployeeRolesController> logger)
    {
        this.employeeRoleService = employeeRoleService;
        this.logger = logger;
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await employeeRoleService.GetRolesByOrganizationAsync(organizationId);
        return result.IsSuccess
            ? Ok(result.Value.Adapt<List<EmployeeRoleDto>>())
            : BadRequest(result.Error);
    }

    [HttpGet("organization/usage")]
    public async Task<IActionResult> GetUsageByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await employeeRoleService.GetRoleUsageByOrganizationAsync(organizationId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await employeeRoleService.GetByIdAsync(id);
        return result.IsSuccess
            ? Ok(result.Value.Adapt<EmployeeRoleDto>())
            : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IResult> Create(EmployeeRoleDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var employeeRole = new EmployeeRole
        {
            Name = model.Name.ToUpper(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            OrganizationId = organizationId
        };
        var result = await employeeRoleService.UpsertAsync(employeeRole);
        return result.IsSuccess
            ? Results.Ok(result.Value.Adapt<EmployeeRoleDto>())
            : result.ToProblemDetails();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, EmployeeRoleDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var employeeRole = new EmployeeRole
        {
            Id = id,
            OrganizationId = organizationId,
            Name = model.Name.ToUpper(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
        };
        var result = await employeeRoleService.UpsertAsync(employeeRole);
        return result.IsSuccess
            ? Ok(result.Value.Adapt<EmployeeRoleDto>())
            : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await employeeRoleService.DeleteAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
    }
}