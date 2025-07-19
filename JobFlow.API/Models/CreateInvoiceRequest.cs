namespace JobFlow.API.Models
{
    public class CreateInvoiceRequest
    {
        public Guid OrganizationId { get; set; }
        public Guid OrganizationClientId { get; set; }
        public DateTime DueDate { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    }

    public class InvoiceLineItemDto
    {
        public string Description { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

}
