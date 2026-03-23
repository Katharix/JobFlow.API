using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.API.Mappings;

public static class InvoiceMappingExtensions
{
    public static Invoice ToInvoice(this CreateInvoiceRequest request, string invoiceNumber)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            OrganizationClientId = request.OrganizationClientId
                ?? throw new InvalidOperationException("Organization client is required."),
            JobId = request.JobId,
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

    public static InvoiceDto ToDto(this Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OrganizationId = invoice.OrganizationId,
            OrganizationClientId = invoice.OrganizationClientId,
            JobId = invoice.JobId,
            OrderId = invoice.OrderId,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            BalanceDue = invoice.BalanceDue,
            Status = invoice.Status,
            PaymentProvider = invoice.PaymentProvider,
            ExternalPaymentId = invoice.ExternalPaymentId,
            PaidAt = invoice.PaidAt,
            OrganizationClient = invoice.OrganizationClient == null
                ? new OrganizationClientDto { Id = invoice.OrganizationClientId }
                : new OrganizationClientDto
                {
                    Id = invoice.OrganizationClient.Id,
                    OrganizationId = invoice.OrganizationClient.OrganizationId,
                    FirstName = invoice.OrganizationClient.FirstName,
                    LastName = invoice.OrganizationClient.LastName,
                    EmailAddress = invoice.OrganizationClient.EmailAddress,
                    PhoneNumber = invoice.OrganizationClient.PhoneNumber,
                    Address1 = invoice.OrganizationClient.Address1,
                    Address2 = invoice.OrganizationClient.Address2,
                    City = invoice.OrganizationClient.City,
                    State = invoice.OrganizationClient.State,
                    ZipCode = invoice.OrganizationClient.ZipCode,
                    Organization = invoice.OrganizationClient.Organization == null
                        ? null
                        : new OrganizationDto
                        {
                            Id = invoice.OrganizationClient.Organization.Id,
                            OrganizationName = invoice.OrganizationClient.Organization.OrganizationName
                        }
                },
            LineItems = invoice.LineItems.Select(li => li.ToDto()).ToList()
        };
    }

    public static InvoiceLineItemDto ToDto(this InvoiceLineItem item)
    {
        return new InvoiceLineItemDto
        {
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.Quantity * item.UnitPrice
        };
    }
}