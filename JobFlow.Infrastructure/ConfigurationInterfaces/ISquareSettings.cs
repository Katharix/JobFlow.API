namespace JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;

public interface ISquareSettings
{
    string? ApplicationId { get; set; }
    string? AccessToken { get; set; }
    string? LocationId { get; set; }
}