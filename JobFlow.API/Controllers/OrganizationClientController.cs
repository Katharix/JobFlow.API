using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[Route("api/organization/clients/")]
[ApiController]
public class OrganizationClientController : ControllerBase
{
    private readonly IOrganizationClientService organizationClientService;
    private readonly IOrganizationClientPortalService _clientPortal;
    private readonly IMapper _mapper;

    public OrganizationClientController(
        IOrganizationClientService organizationClientService,
        IOrganizationClientPortalService clientPortal,
        IMapper mapper)
    {
        this.organizationClientService = organizationClientService;
        _clientPortal = clientPortal;
        _mapper = mapper;
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
        var organizationId = HttpContext.GetOrganizationId();
        var result = await organizationClientService.DeleteClient(clientId, organizationId);
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
        var entity = _mapper.Map<OrganizationClient>(model);

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

    [HttpPost("{organizationClientId:guid}/send-client-hub-link")]
    public async Task<IResult> SendClientHubLink(Guid organizationClientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var clientResult = await organizationClientService.GetClientById(organizationClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(clientResult.Value.EmailAddress))
            return Results.BadRequest("Client email address is required.");

        var result = await _clientPortal.SendMagicLinkWithUrlAsync(
            organizationId,
            organizationClientId,
            clientResult.Value.EmailAddress);

        return result.IsSuccess
            ? Results.Ok(new { magicLink = result.Value })
            : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("restore")]
    public async Task<IResult> RestoreClient(Guid clientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await organizationClientService.RestoreClient(clientId, organizationId);
        return result.IsSuccess ? Results.Ok(result) : result.ToProblemDetails();
    }
}