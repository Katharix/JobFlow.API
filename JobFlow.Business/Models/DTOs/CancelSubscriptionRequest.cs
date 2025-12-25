namespace JobFlow.Business.Models.DTOs;

public class CancelSubscriptionRequest
{
    public string ProviderSubscriptionId { get; set; } = string.Empty;
    public DateTime CanceledAt { get; set; } = DateTime.UtcNow;
}