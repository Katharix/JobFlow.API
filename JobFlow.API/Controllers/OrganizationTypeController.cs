using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JobFlow.API.Controllers;

[Route("api/organization/types/")]
[ApiController]
public class OrganizationTypeController : ControllerBase
{
    private readonly ITwilioService _twilioService;
    private readonly IOrganizationTypeService organizationTypeService;

    public OrganizationTypeController(IOrganizationTypeService organizationTypeService, ITwilioService twilioService)
    {
        this.organizationTypeService = organizationTypeService;
        _twilioService = twilioService;
    }

    [HttpGet]
    [Route("all")]
    [AllowAnonymous]
    public async Task<IResult> GetAllOrganizationTypes()
    {
        var result = await organizationTypeService.GetTypes();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet]
    [Route("id")]
    [AllowAnonymous]
    public async Task<IResult> GetTypeById(Guid organizationTypeId)
    {
        var result = await organizationTypeService.GetTypeById(organizationTypeId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("upsert")]
    public async Task<IResult> UpsertOrganization(OrganizationType model)
    {
        var result = await organizationTypeService.UpsertOrganizationType(model);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("upsert/list")]
    public async Task<IResult> UpsertOrganizations(IEnumerable<OrganizationType> modelList)
    {
        var result = await organizationTypeService.UpsertOrganizationList(modelList);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete]
    [Route("delete")]
    public async Task<IResult> DeleteOrganizationType(Guid organizationTypeId)
    {
        var result = await organizationTypeService.DeleteOrganizationType(organizationTypeId);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpDelete]
    [Route("delete/list")]
    public async Task<IResult> DeleteMultipleOrganizationTypes(IEnumerable<Guid> idList)
    {
        var result = await organizationTypeService.DeleteMultipleOrganizationTypes(idList);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }
}