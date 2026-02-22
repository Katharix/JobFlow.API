using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await employeeRoleService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IResult> Create(EmployeeRoleDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var employeeRole = new EmployeeRole
        {
            Name = model.Name.ToUpper(),
            OrganizationId = organizationId
        };
        var result = await employeeRoleService.UpsertAsync(employeeRole);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, EmployeeRole model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        model.Id = id;
        model.OrganizationId = organizationId;
        var result = await employeeRoleService.UpsertAsync(model);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await employeeRoleService.DeleteAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
    }
}