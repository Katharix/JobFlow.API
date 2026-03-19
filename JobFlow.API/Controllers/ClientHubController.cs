using JobFlow.API.Extensions;
using JobFlow.API.Hubs;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/client-hub")]
[Authorize(AuthenticationSchemes = "ClientPortalJwt", Policy = "OrganizationClientOnly")]
public class ClientHubController : ControllerBase
{
    private readonly IEstimateService _estimates;
    private readonly IEstimateRevisionService _estimateRevisions;
    private readonly IInvoiceService _invoices;
    private readonly IOrganizationClientService _clients;
    private readonly IHubContext<NotifierHub> _hubContext;

    public ClientHubController(
        IEstimateService estimates,
        IEstimateRevisionService estimateRevisions,
        IInvoiceService invoices,
        IOrganizationClientService clients,
        IHubContext<NotifierHub> hubContext)
    {
        _estimates = estimates;
        _estimateRevisions = estimateRevisions;
        _invoices = invoices;
        _clients = clients;
        _hubContext = hubContext;
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

    [HttpPost("estimates/{id:guid}/accept")]
    public async Task<IResult> AcceptEstimate(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.AcceptAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("estimates/{id:guid}/decline")]
    public async Task<IResult> DeclineEstimate(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimates.DeclineAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("estimates/{id:guid}/revision-requests")]
    [RequestSizeLimit(55_000_000)]
    public async Task<IResult> RequestEstimateRevision(Guid id, [FromForm] CreateEstimateRevisionFormRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var attachments = new List<EstimateRevisionAttachmentUpload>();

        if (request.Attachments is not null)
        {
            foreach (var file in request.Attachments)
            {
                if (file.Length <= 0)
                    continue;

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                attachments.Add(new EstimateRevisionAttachmentUpload(
                    file.FileName,
                    file.ContentType,
                    stream.ToArray(),
                    file.Length));
            }
        }

        var result = await _estimateRevisions.CreateAsync(
            id,
            organizationId,
            orgClientId,
            new CreateEstimateRevisionRequest(request.Message ?? string.Empty, attachments));

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        await _hubContext.Clients.Group($"org:{organizationId}:dashboard")
            .SendAsync("EstimateRevisionRequested", new
            {
                estimateId = id,
                revisionRequestId = result.Value.Id,
                revisionNumber = result.Value.RevisionNumber,
                requestedAt = result.Value.RequestedAt,
                message = result.Value.RequestMessage
            });

        return Results.Ok(result.Value);
    }

    [HttpGet("estimates/{id:guid}/revision-requests")]
    public async Task<IResult> GetEstimateRevisionRequests(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimateRevisions.GetByEstimateAsync(id, organizationId, orgClientId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("estimates/{estimateId:guid}/revision-requests/{revisionRequestId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IResult> DownloadEstimateRevisionAttachment(Guid estimateId, Guid revisionRequestId, Guid attachmentId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var orgClientId = HttpContext.GetUserId();

        var result = await _estimateRevisions.GetAttachmentAsync(
            estimateId,
            revisionRequestId,
            attachmentId,
            organizationId,
            orgClientId);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
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

public record CreateEstimateRevisionFormRequest(
    string? Message,
    List<IFormFile>? Attachments);
