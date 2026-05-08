using JobFlow.Business.DI;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Integrations.QuickBooks;

public interface IQuickBooksIntegrationService
{
    Task<QuickBooksConnectionStatus?> GetStatusAsync(Guid organizationId);
    Task<QuickBooksConnection> ConnectAsync(
        Guid organizationId,
        string realmId,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiresAtUtc,
        DateTime refreshTokenExpiresAtUtc);
    Task DisconnectAsync(Guid organizationId);
    Task UpdateLastSyncedAsync(Guid organizationId);
}

public record QuickBooksConnectionStatus(
    bool IsConnected,
    string? RealmId,
    DateTime? LastSyncedAtUtc,
    DateTime? TokenExpiresAtUtc);

[ScopedService]
public class QuickBooksIntegrationService : IQuickBooksIntegrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuickBooksTokenEncryptionService _encryption;
    private readonly ILogger<QuickBooksIntegrationService> _logger;

    public QuickBooksIntegrationService(
        IUnitOfWork unitOfWork,
        IQuickBooksTokenEncryptionService encryption,
        ILogger<QuickBooksIntegrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<QuickBooksConnectionStatus?> GetStatusAsync(Guid organizationId)
    {
        var connection = await _unitOfWork.RepositoryOf<QuickBooksConnection>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId);

        if (connection is null)
            return new QuickBooksConnectionStatus(false, null, null, null);

        return new QuickBooksConnectionStatus(
            connection.IsConnected,
            connection.RealmId,
            connection.LastSyncedAtUtc,
            connection.TokenExpiresAtUtc);
    }

    public async Task<QuickBooksConnection> ConnectAsync(
        Guid organizationId,
        string realmId,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiresAtUtc,
        DateTime refreshTokenExpiresAtUtc)
    {
        var repo = _unitOfWork.RepositoryOf<QuickBooksConnection>();

        var existing = await repo.Query()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId);

        if (existing is not null)
        {
            existing.RealmId = realmId;
            existing.EncryptedAccessToken = _encryption.Encrypt(accessToken);
            existing.EncryptedRefreshToken = _encryption.Encrypt(refreshToken);
            existing.TokenExpiresAtUtc = tokenExpiresAtUtc;
            existing.RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
            existing.IsConnected = true;
            existing.IsActive = true;
        }
        else
        {
            existing = new QuickBooksConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RealmId = realmId,
                EncryptedAccessToken = _encryption.Encrypt(accessToken),
                EncryptedRefreshToken = _encryption.Encrypt(refreshToken),
                TokenExpiresAtUtc = tokenExpiresAtUtc,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                IsConnected = true
            };
            await repo.AddAsync(existing);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("QuickBooks connected for org {OrgId}, realm {RealmId}", organizationId, realmId);
        return existing;
    }

    public async Task DisconnectAsync(Guid organizationId)
    {
        var connection = await _unitOfWork.RepositoryOf<QuickBooksConnection>()
            .Query()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId);

        if (connection is null) return;

        connection.IsConnected = false;
        connection.EncryptedAccessToken = null;
        connection.EncryptedRefreshToken = null;
        connection.TokenExpiresAtUtc = null;

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("QuickBooks disconnected for org {OrgId}", organizationId);
    }

    public async Task UpdateLastSyncedAsync(Guid organizationId)
    {
        var connection = await _unitOfWork.RepositoryOf<QuickBooksConnection>()
            .Query()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsConnected);

        if (connection is null) return;

        connection.LastSyncedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }
}
