using JobFlow.API.Mappings;
using JobFlow.API.Models;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService invoiceService;
        private readonly IInvoiceLineItemService lineItemService;
        private readonly IInvoiceNumberGenerator numberGenerator;
        private readonly INotificationService notificationService;
        private readonly IPdfGenerator pdfGenerator;

        public InvoiceController(
            IInvoiceService invoiceService,
            IInvoiceLineItemService lineItemService,
            IInvoiceNumberGenerator numberGenerator,
            IPdfGenerator pdfGenerator,
            INotificationService notificationService)
        {
            this.invoiceService = invoiceService;
            this.lineItemService = lineItemService;
            this.numberGenerator = numberGenerator;
            this.pdfGenerator = pdfGenerator;
            this.notificationService = notificationService;
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

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] CreateInvoiceRequest request)
        {
            var invoiceNumber = await numberGenerator.GenerateAsync(request.OrganizationId);
            var invoice = request.ToInvoice(invoiceNumber);

            var result = await invoiceService.UpsertInvoiceAsync(invoice);
            return result.IsSuccess ? Ok(result.Value.ToDto()) : BadRequest(result.Error);
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
            await this.notificationService.SendClientInvoiceCreatedNotificationAsync(invoice.OrganizationClient, invoice);
            var pdfName = $"{invoice.OrganizationClient.Organization.OrganizationName}-Invoice-{invoice.InvoiceNumber}.pdf";
            return File(pdf, "application/pdf", $"{pdfName}");
        }

        
    }

}
