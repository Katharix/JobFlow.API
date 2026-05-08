namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface IQuickBooksSettings
{
    string? ClientId { get; set; }
    string? ClientSecret { get; set; }
    string? RedirectUrl { get; set; }
    bool UseSandbox { get; set; }
}
