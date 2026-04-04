namespace JobFlow.Business.Models.DTOs;

public class SubscriptionPlanPriceDto
{
    public string PlanKey { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public string ProviderPriceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
}
