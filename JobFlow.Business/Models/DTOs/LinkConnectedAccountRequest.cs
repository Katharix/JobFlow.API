using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class LinkConnectedAccountRequest
{
    public PaymentProvider Provider { get; set; }
    public string AccountId { get; set; } = string.Empty;
}
