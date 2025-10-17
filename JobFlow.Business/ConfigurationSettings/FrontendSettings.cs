using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;

namespace JobFlow.Business.ConfigurationSettings
{
    public class FrontEndSettings : IFrontendSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
