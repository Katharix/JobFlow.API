namespace JobFlow.Business.Models.DTOs;

public class ChangeSubscriptionPlanRequest
{
    public string ProviderSubscriptionId { get; set; } = string.Empty;
    public string ProviderPriceId { get; set; } = string.Empty;
}
