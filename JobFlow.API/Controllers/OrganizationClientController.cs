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

    [HttpGet("all/{organizationId:guid}")]
    public async Task<IResult> GetAllClientsByOrganizationId(
        [FromRoute] Guid organizationId)
    {
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

    [HttpPost("{organizationId:guid}/upsert")]
    public async Task<IResult> UpsertClient(
        [FromRoute] Guid organizationId,
        [FromBody] OrganizationClientDto model)
    {
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