using JobFlow.API.Extensions;
using JobFlow.API.Mappings;
using JobFlow.API.Models;
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
    private readonly IInvoiceNumberGenerator numberGenerator;
    private readonly IPdfGenerator pdfGenerator;
    private readonly IMapper _mapper;

    public InvoiceController(
        IInvoiceService invoiceService,
        IInvoiceLineItemService lineItemService,
        IInvoiceNumberGenerator numberGenerator,
        IPdfGenerator pdfGenerator,
        INotificationService notificationService,
        IJobService jobService,
        IMapper mapper
        )
    {
        this.invoiceService = invoiceService;
        this.lineItemService = lineItemService;
        this.numberGenerator = numberGenerator;
        this.pdfGenerator = pdfGenerator;
        this.notificationService = notificationService;
        this._jobService = jobService;
        this._mapper = mapper;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        var value = _mapper.Map<InvoiceDto>(result.Value);
        return result.IsSuccess ? Ok(value) : NotFound(result.Error);
    }

    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetByClient(Guid clientId)
    {
        var result = await invoiceService.GetInvoicesByClientAsync(clientId);
        return Ok(result.Value.ToDto());
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetByOrganization()
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await invoiceService.GetInvoicesByOrganizationAsync(organizationId);
        return result.IsSuccess ? Ok(result.Value.ToDto()) : BadRequest(result.Error);
    }

    [HttpPost("{organizationId:guid}")]
    public async Task<IActionResult> Upsert(
        [FromRoute] Guid organizationId,
        [FromBody] CreateInvoiceRequest request)
    {
        var invoiceNumber = await numberGenerator.GenerateAsync(organizationId);

        var jobInfo = await this._jobService.GetJobByIdAsync(request.JobId, organizationId);
        
        request.OrganizationClientId = jobInfo.Value.OrganizationClientId;
        var invoice = request.ToInvoice(invoiceNumber);
        invoice.OrganizationId = organizationId;

        var result = await invoiceService.UpsertInvoiceAsync(invoice);

        return result.IsSuccess
            ? Ok(result.Value.ToDto())
            : BadRequest(result.Error);
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
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var invoice = result.Value;

        await notificationService.SendClientInvoiceCreatedNotificationAsync(
            invoice.OrganizationClient,
            invoice
        );

        await invoiceService.MarkInvoiceSentAsync(invoice.Id);

        return Ok();
    }



    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Optional: delete line items too
        await lineItemService.DeleteByInvoiceIdAsync(id);
        var result = await invoiceService.DeleteInvoiceAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);
        //46455c4d-58c0-49ef-b18a-84704dbd50aa
        var pdf = await pdfGenerator.GenerateInvoicePdfAsync(result.Value);
        var invoice = result.Value;
        await notificationService.SendClientInvoiceCreatedNotificationAsync(invoice.OrganizationClient, invoice);
        var pdfName = $"{invoice.OrganizationClient.Organization.OrganizationName}-Invoice-{invoice.InvoiceNumber}.pdf";
        return File(pdf, "application/pdf", $"{pdfName}");
    }
}