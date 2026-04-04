using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class SupportHubFinancialSummaryDto
{
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public PaymentProvider PaymentProvider { get; set; }
    public decimal GrossCollected { get; set; }
    public decimal Refunded { get; set; }
    public decimal NetCollected { get; set; }
    public decimal Outstanding { get; set; }
    public int DisputeCount { get; set; }
    public int InvoiceCount { get; set; }
}
