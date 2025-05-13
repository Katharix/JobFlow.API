using JobFlow.API.Models;
using JobFlow.Domain.Models;

namespace JobFlow.API.Mappings
{
    public static class InvoiceMappingExtensions
    {
        public static Invoice ToInvoice(this CreateInvoiceRequest request, string invoiceNumber)
        {
            return new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                OrganizationClientId = request.OrganizationClientId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.UtcNow,
                DueDate = request.DueDate,
                LineItems = request.LineItems.Select(li => new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice
                }).ToList()
            };
        }
        public static IEnumerable<InvoiceDto> ToDto(this IEnumerable<Invoice> invoices)
        {
            return invoices.Select(invoice => invoice.ToDto());
        }

        public static InvoiceDto ToDto(this Invoice invoice) =>
           new InvoiceDto
           {
               Id = invoice.Id,
               InvoiceNumber = invoice.InvoiceNumber,
               OrganizationId = invoice.OrganizationId,
               OrganizationClientId = invoice.OrganizationClientId,
               OrderId = invoice.OrderId,
               InvoiceDate = invoice.InvoiceDate,
               DueDate = invoice.DueDate,
               TotalAmount = invoice.TotalAmount,
               AmountPaid = invoice.AmountPaid,
               BalanceDue = invoice.BalanceDue,
               Status = invoice.Status,
               StripeInvoiceId = invoice.StripeInvoiceId,
               OrganizationClient = invoice.OrganizationClient.ToDto(),
               LineItems = invoice.LineItems.Select(li => li.ToDto()).ToList(),
           };

        public static InvoiceLineItemDto ToDto(this InvoiceLineItem item) =>
            new InvoiceLineItemDto
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            };


    }

}
