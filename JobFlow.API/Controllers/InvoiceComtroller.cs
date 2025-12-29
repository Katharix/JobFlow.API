using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Services.ServiceInterfaces;
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

    public InvoiceController(
        IInvoiceService invoiceService,
        IInvoiceLineItemService lineItemService,
        IInvoiceNumberGenerator numberGenerator,
        IPdfGenerator pdfGenerator,
        INotificationService notificationService,
        IJobService jobService)
    {
        this.invoiceService = invoiceService;
        this.lineItemService = lineItemService;
        this.numberGenerator = numberGenerator;
        this.pdfGenerator = pdfGenerator;
        this.notificationService = notificationService;
        this._jobService = jobService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await invoiceService.GetInvoiceByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value.ToDto()) : NotFound(result.Error);
    }

    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetByClient(Guid clientId)
    {
        var result = await invoiceService.GetInvoicesByClientAsync(clientId);
        return Ok(result.Value.ToDto());
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



    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Optional: delete line items too
        await lineItemService.DeleteByInvoiceIdAsync(id);
        var result = await invoiceService.DeleteInvoiceAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpGet("{id}/pdf")]
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