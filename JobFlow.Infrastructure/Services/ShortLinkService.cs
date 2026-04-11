using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Utilities;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Services;

[ScopedService]
public class ShortLinkService : IShortLinkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackendSettings _backendSettings;
    private readonly ILogger<ShortLinkService> _logger;

    public ShortLinkService(IUnitOfWork unitOfWork, IBackendSettings backendSettings, ILogger<ShortLinkService> logger)
    {
        _unitOfWork = unitOfWork;
        _backendSettings = backendSettings;
        _logger = logger;
    }

    public async Task<string> CreateAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.RepositoryOf<ShortLink>();
        var code = ShortCodeGenerator.Generate(8);

        // Ensure uniqueness
        while (await repo.QueryWithNoTracking().AnyAsync(s => s.Code == code, cancellationToken))
            code = ShortCodeGenerator.Generate(8);

        await repo.AddAsync(new ShortLink
        {
            Code = code,
            TargetUrl = targetUrl,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        return $"{_backendSettings.BaseUrl.TrimEnd('/')}/l/{code}";
    }

    public async Task<string?> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.RepositoryOf<ShortLink>();
        var link = await repo.Query()
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

        if (link is null)
            return null;

        link.AccessCount++;
        link.LastAccessedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return link.TargetUrl;
    }
}
