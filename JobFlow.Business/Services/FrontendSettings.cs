using JobFlow.Business.Services.ServiceInterfaces;

namespace JobFlow.Business.Services
{
    public class FrontEndSettings : IFrontendSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
