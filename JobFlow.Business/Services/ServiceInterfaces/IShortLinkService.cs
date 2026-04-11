namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IShortLinkService
{
    Task<string> CreateAsync(string targetUrl, CancellationToken cancellationToken = default);
    Task<string?> ResolveAsync(string code, CancellationToken cancellationToken = default);
}
