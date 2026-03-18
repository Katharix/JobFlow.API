using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace JobFlow.Business.Services;

[ScopedService]
public class OrganizationClientPortalService : IOrganizationClientPortalService
{
    private readonly ILogger<OrganizationClientPortalService> _logger;
    private readonly IFrontendSettings _frontend;
    private readonly IRepository<OrganizationClient> _clients;
    private readonly IRepository<OrganizationClientPortalSession> _sessions;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationClientPortalService(
        ILogger<OrganizationClientPortalService> logger,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        IFrontendSettings frontend)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _frontend = frontend;

        _clients = unitOfWork.RepositoryOf<OrganizationClient>();
        _sessions = unitOfWork.RepositoryOf<OrganizationClientPortalSession>();
    }

    public async Task<Result> SendMagicLinkAsync(Guid organizationId, Guid organizationClientId, string emailAddress)
    {
        if (organizationId == Guid.Empty || organizationClientId == Guid.Empty)
            return Result.Failure(Error.Failure("OrganizationClientPortal", "Organization and client are required."));

        if (string.IsNullOrWhiteSpace(emailAddress))
            return Result.Failure(Error.Failure("OrganizationClientPortal", "Email is required."));

        var client = await _clients.Query()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == organizationClientId && x.OrganizationId == organizationId);

        if (client is null)
            return Result.Failure(Error.NotFound("OrganizationClientPortal", "Client not found."));

        if (!string.Equals(client.EmailAddress, emailAddress, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Failure("OrganizationClientPortal", "Email does not match client record."));

        var token = OrganizationClientPortalSession.GenerateToken();
        var tokenHash = HashToken(token);

        var session = new OrganizationClientPortalSession
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OrganizationClientId = organizationClientId,
            EmailAddress = emailAddress,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await _sessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();
        var url = $"{_frontend.BaseUrl}/client-hub/auth?token={token}";

        await _notifications.SendOrganizationClientPortalMagicLinkAsync(client, url);

        return Result.Success();
    }

    public async Task<Result<OrganizationClient>> RedeemMagicLinkAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<OrganizationClient>(Error.Failure("OrganizationClientPortal", "Token is required."));

        var tokenHash = HashToken(token);

        var session = await _sessions.Query()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (session is null)
            return Result.Failure<OrganizationClient>(Error.Failure("OrganizationClientPortal", "Invalid or expired link."));

        if (session.RedeemedAt.HasValue || session.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result.Failure<OrganizationClient>(Error.Failure("OrganizationClientPortal", "Invalid or expired link."));

        session.RedeemedAt = DateTimeOffset.UtcNow;
        _sessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        var client = await _clients.Query()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == session.OrganizationClientId && x.OrganizationId == session.OrganizationId);

        if (client is null)
            return Result.Failure<OrganizationClient>(Error.NotFound("OrganizationClientPortal", "Client not found."));

        return Result.Success(client);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
