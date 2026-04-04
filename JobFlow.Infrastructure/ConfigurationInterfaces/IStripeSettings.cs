namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface IStripeSettings
{
    string ApiKey { get; set; }
    string ReturnUrl { get; set; }
    string RefreshUrl { get; set; }
    string WebhookKey { get; set; }
    string GoMonthlyPrice { get; set; }
    string GoYearlyPrice { get; set; }
    string FlowMonthlyPrice { get; set; }
    string FlowYearlyPrice { get; set; }
    string MaxMonthlyPrice { get; set; }
    string MaxYearlyPrice { get; set; }
}