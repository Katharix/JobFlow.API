using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/client-hub")]
[Authorize(AuthenticationSchemes = "ClientPortalJwt", Policy = "OrganizationClientOnly")]
public class ClientHubController : ControllerBase
{
    private readonly IEstimateService _estimates;
    private readonly IInvoiceService _invoices;
    private readonly IOrganizationClientService _clients;

    public ClientHubController(
        IEstimateService estimates,
        IInvoiceService invoices,
        IOrganizationClientService clients)
    {
        _estimates = estimates;
        _invoices = invoices;
        _clients = clients;
    }

    [HttpGet("me")]
    public async Task<IResult> Me()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        if (clientResult.Value.OrganizationId != organizationId)
            return Results.Unauthorized();

        return Results.Ok(clientResult.Value);
    }

    [HttpPut("me")]
    public async Task<IResult> UpdateMe([FromBody] UpdateOrganizationClientRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var clientResult = await _clients.GetClientById(orgClientId);
        if (!clientResult.IsSuccess)
            return clientResult.ToProblemDetails();

        var client = clientResult.Value;
        if (client.OrganizationId != organizationId)
            return Results.Unauthorized();

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        client.EmailAddress = request.EmailAddress;
        client.PhoneNumber = request.PhoneNumber;
        client.Address1 = request.Address1;
        client.Address2 = request.Address2;
        client.City = request.City;
        client.State = request.State;
        client.ZipCode = request.ZipCode;

        var upsert = await _clients.UpsertClient(client);
        return upsert.IsSuccess ? Results.Ok(upsert.Value) : upsert.ToProblemDetails();
    }

    [HttpGet("estimates")]
    public async Task<IResult> GetMyEstimates()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var orgEstimates = await _estimates.GetByOrganizationAsync(organizationId);
        if (!orgEstimates.IsSuccess)
            return orgEstimates.ToProblemDetails();

        var mine = orgEstimates.Value.Where(x => x.OrganizationClientId == orgClientId);
        return Results.Ok(mine);
    }

    [HttpGet("estimates/{id:guid}")]
    public async Task<IResult> GetMyEstimateById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.GetByIdAsync(id);
        if (!result.IsSuccess)
            return result.ToProblemDetails();

        var estimate = result.Value;
        if (estimate.OrganizationId != organizationId || estimate.OrganizationClientId != orgClientId)
            return Results.NotFound();

        return Results.Ok(estimate);
    }

    [HttpGet("invoices")]
    public async Task<IResult> GetMyInvoices()
    {
        var orgClientId = HttpContext.GetUserId();
        var result = await _invoices.GetInvoicesByClientAsync(orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }
}

public record UpdateOrganizationClientRequest(
    string? FirstName,
    string? LastName,
    string? EmailAddress,
    string? PhoneNumber,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode);
