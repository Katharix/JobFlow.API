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
    }

}
