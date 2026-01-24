using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[Route("api/organization/clients/")]
[ApiController]
public class OrganizationClientController : ControllerBase
{
    private readonly IOrganizationClientService organizationClientService;

    public OrganizationClientController(IOrganizationClientService organizationClientService)
    {
        this.organizationClientService = organizationClientService;
    }

    [HttpGet]
    [Route("all")]
    public async Task<IResult> GetAllClients()
    {
        var result = await organizationClientService.GetAllClients();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("orgall")]
    public async Task<IResult> GetAllClientsByOrganizationId()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await organizationClientService.GetAllClientsByOrganizationId(organizationId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }


    [HttpDelete]
    [Route("delete")]
    public async Task<IResult> DeleteClient(Guid clientId)
    {
        var result = await organizationClientService.DeleteClient(clientId);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }

    [HttpPost("upsert")]
    public async Task<IResult> UpsertClient(
        [FromBody] OrganizationClientDto model)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Results.BadRequest("OrganizationId is required.");

        model.OrganizationId = organizationId;
        var entity = model.ToEntity();

        var result = await organizationClientService.UpsertClient(entity);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblemDetails();
    }


    [HttpPost]
    [Route("upsert/multi")]
    public async Task<IResult> UpsertMultipleClients(IEnumerable<OrganizationClient> modelList)
    {
        var result = await organizationClientService.UpsertMultipleClients(modelList);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}