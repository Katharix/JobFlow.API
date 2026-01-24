using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet("organization")]
    public async Task<IResult> GetByOrganizationId()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _employeeService.GetByOrganizationIdAsync(organizationId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id}")]
    public async Task<IResult> GetById(Guid id)
    {
        var result = await _employeeService.GetByIdAsync(id);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    public async Task<IResult> Create(CreateEmployeeRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        request.OrganizationId = organizationId;
        var result = await _employeeService.CreateAsync(request);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id}")]
    public async Task<IResult> Update(Guid id, UpdateEmployeeRequest request)
    {
        var result = await _employeeService.UpdateAsync(id, request);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(Guid id)
    {
        var result = await _employeeService.DeleteAsync(id);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpGet("email/{email}")]
    public async Task<IResult> EmployeeEmailExist(string email)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _employeeService.EmployeeExistByEmailAsync(organizationId, email);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}