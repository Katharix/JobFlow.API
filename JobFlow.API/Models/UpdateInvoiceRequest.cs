namespace JobFlow.API.Models;

public class UpdateInvoiceRequest
{
    public DateTime? InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
}
