using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Hosting;

namespace JobFlow.API.Services;

/// <summary>
/// Bridges <see cref="IWebHostEnvironment"/> into the Business layer via the
/// <see cref="IWebHostEnvironmentAccessor"/> abstraction.
/// </summary>
public class WebHostEnvironmentAccessor : IWebHostEnvironmentAccessor
{
    private readonly IWebHostEnvironment _env;

    public WebHostEnvironmentAccessor(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string WebRootPath => _env.WebRootPath ?? _env.ContentRootPath;
    public string ContentRootPath => _env.ContentRootPath;
}
