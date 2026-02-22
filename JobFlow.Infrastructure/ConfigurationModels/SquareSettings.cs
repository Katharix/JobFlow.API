using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.ConfigurationModels;

public class SquareSettings : ISquareSettings
{
    public string? ApplicationId { get; set; }
    public string? AccessToken { get; set; }
    public string? LocationId { get; set; }
}