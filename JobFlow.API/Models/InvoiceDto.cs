using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;

namespace JobFlow.API.Models;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OrganizationClientId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? OrderId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public InvoiceStatus Status { get; set; }
    public PaymentProvider PaymentProvider { get; set; }
    public string? ExternalPaymentId { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public OrganizationClientDto OrganizationClient { get; set; } = null!;
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
}