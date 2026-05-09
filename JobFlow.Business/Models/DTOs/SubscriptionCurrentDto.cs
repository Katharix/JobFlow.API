namespace JobFlow.Business.Models.DTOs;

public class SubscriptionCurrentDto
{
    public Guid Id { get; set; }
    public string ProviderSubscriptionId { get; set; } = string.Empty;
    public string ProviderPriceId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime StartDate { get; set; }
    public DateTime? CanceledAt { get; set; }
    public int? SeatLimit { get; set; }
    public string ResolvedPlanKey { get; set; } = string.Empty;
    public string ResolvedBillingCycle { get; set; } = string.Empty;
}
