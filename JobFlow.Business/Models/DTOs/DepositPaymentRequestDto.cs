using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class DepositPaymentRequestDto
{
    public PaymentProvider Provider { get; set; }
    public Guid OrganizationClientId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string ProductName { get; set; } = "Deposit";
    public decimal Amount { get; set; }
}
