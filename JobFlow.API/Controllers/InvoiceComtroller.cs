using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService invoiceService;
    private readonly IInvoiceLineItemService lineItemService;
    private readonly IJobService _jobService;
    private readonly INotificationService notificationService;
    private readonly IOrganizationClientPortalService _clientPortal;
    private readonly IInvoiceNumberGenerator numberGenerator;
    private readonly IPdfGenerator pdfGenerator;
    private readonly IMapper _mapper;

    public InvoiceController(
        IInvoiceService invoiceService,
        IInvoiceLineItemService lineItemService,
        IInvoiceNumberGenerator numberGenerator,
        IPdfGenerator pdfGenerator,
        INotificationService notificationService,
        IOrganizationClientPortalService clientPortal,
        IJobService jobService,
        IMapper mapper
        )
    {
        this.invoiceService = invoiceService;
        this.lineItemService = lineItemService;
        this.numberGenerator = numberGenerator;
        this.pdfGenerator = pdfGenerator;
        this.notificationService = notificationService;
        _clientPortal = clientPortal;
        this._jobService = jobService;
        this._mapper = mapper;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var organizationId = HttpContext.GetOrganizationId();
        if (result.Value.OrganizationId != organizationId)
            return NotFound();

        var value = _mapper.Map<InvoiceDto>(result.Value);
        return Ok(value);
    }

    [HttpGet("organization/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await invoiceService.GetInvoiceAggregatesByOrganizationAsync(organizationId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetByClient(Guid clientId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await invoiceService.GetInvoicesByClientAsync(clientId);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var filtered = result.Value.Where(i => i.OrganizationId == organizationId).ToList();
        return Ok(filtered.ToDto());
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetByOrganization(
        [FromQuery] string? cursor = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var hasFilters = !string.IsNullOrWhiteSpace(status)
            || !string.IsNullOrWhiteSpace(search)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection);

        if (!pageSize.HasValue && string.IsNullOrWhiteSpace(cursor) && !hasFilters)
        {
            var legacyResult = await invoiceService.GetInvoicesByOrganizationAsync(organizationId);
            return legacyResult.IsSuccess ? Ok(legacyResult.Value.ToDto()) : BadRequest(legacyResult.Error);
        }

        var result = await invoiceService.GetInvoicesByOrganizationPagedAsync(
            organizationId,
            Math.Clamp(pageSize ?? 50, 1, 100),
            cursor,
            status,
            search,
            sortBy,
            sortDirection);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new CursorPagedResponseDto<InvoiceDto>
        {
            Items = result.Value.Items.ToDto().ToList(),
            NextCursor = result.Value.NextCursor,
            TotalCount = result.Value.TotalCount
        });
    }

    [HttpPost("{organizationId:guid}")]
    public async Task<IActionResult> Upsert(
        [FromRoute] Guid organizationId,
        [FromBody] CreateInvoiceRequest request)
    {
        var invoiceNumber = await numberGenerator.GenerateAsync(organizationId);

        var jobInfo = await this._jobService.GetJobByIdAsync(request.JobId, organizationId);
        if (!jobInfo.IsSuccess)
            return BadRequest(jobInfo.Error);

        request.OrganizationClientId = jobInfo.Value.OrganizationClientId;
        var invoice = request.ToInvoice(invoiceNumber);
        invoice.OrganizationId = organizationId;

        var result = await invoiceService.UpsertInvoiceAsync(invoice);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var hydratedInvoice = await invoiceService.GetInvoiceByIdAsync(result.Value.Id);
        if (hydratedInvoice.IsSuccess)
        {
            await invoiceService.SendInvoiceToClientAsync(hydratedInvoice.Value.Id);
        }

        return Ok(result.Value.ToDto());
    }

    [HttpPost("organization")]
    public Task<IActionResult> UpsertForOrganization([FromBody] CreateInvoiceRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Task.FromResult<IActionResult>(Unauthorized("Organization context missing."));

        return Upsert(organizationId, request);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> SendInvoice(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);
        if (result.Value.OrganizationId != organizationId)
            return NotFound();

        await invoiceService.SendInvoiceToClientAsync(result.Value.Id);

        return Ok();
    }

    [HttpPost("{id:guid}/remind")]
    public async Task<IActionResult> SendInvoiceReminder(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var invoice = result.Value;
        if (invoice.OrganizationId != organizationId)
            return NotFound();

        if (invoice.OrganizationClient is null)
            return BadRequest("Invoice client is missing.");

        var client = invoice.OrganizationClient;
        string? linkOverride = null;
        var email = client.EmailAddress;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var returnUrl = $"/client-hub/invoices/{invoice.Id}";
            var linkResult = await _clientPortal.CreateMagicLinkAsync(
                invoice.OrganizationId,
                invoice.OrganizationClientId,
                email,
                returnUrl);

            if (linkResult.IsSuccess)
            {
                linkOverride = linkResult.Value;
            }
        }

        await notificationService.SendClientInvoiceReminderNotificationAsync(
            client,
            invoice,
            linkOverride
        );

        return Ok();
    }



    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var existing = await invoiceService.GetInvoiceByIdAsync(id);
        if (!existing.IsSuccess)
            return NotFound(existing.Error);
        if (existing.Value.OrganizationId != organizationId)
            return NotFound();

        await lineItemService.DeleteByInvoiceIdAsync(id);
        var result = await invoiceService.DeleteInvoiceAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);
        if (result.Value.OrganizationId != organizationId)
            return NotFound();
        //46455c4d-58c0-49ef-b18a-84704dbd50aa
        var pdf = await pdfGenerator.GenerateInvoicePdfAsync(result.Value);
        var invoice = result.Value;
        var pdfName = $"{invoice.OrganizationClient.Organization.OrganizationName}-Invoice-{invoice.InvoiceNumber}.pdf";
        return File(pdf, "application/pdf", $"{pdfName}");
    }
}