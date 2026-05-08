using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class QuickBooksSettings : IQuickBooksSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public bool UseSandbox { get; set; }
}
