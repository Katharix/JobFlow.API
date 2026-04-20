namespace JobFlow.Business.Services.ServiceInterfaces;

/// <summary>
/// Abstracts access to the web host environment's paths.
/// Implemented in the API layer using <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment"/>.
/// </summary>
public interface IWebHostEnvironmentAccessor
{
    string WebRootPath { get; }
    string ContentRootPath { get; }
}
