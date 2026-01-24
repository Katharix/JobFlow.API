using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;

namespace JobFlow.Business.ConfigurationSettings;

public class BackendSettings : IBackendSettings
{
    public string BaseUrl { get; set; } = string.Empty;
}