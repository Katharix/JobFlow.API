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

    public async Task<Result> SendMagicLinkAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl = null)
    {
        var result = await CreateMagicLinkInternalAsync(
            organizationId,
            organizationClientId,
            emailAddress,
            returnUrl);

        if (!result.IsSuccess)
            return Result.Failure(result.Error);

        await _notifications.SendOrganizationClientPortalMagicLinkAsync(result.Value.Client, result.Value.Url);

        return Result.Success();
    }

    public async Task<Result<string>> CreateMagicLinkAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl = null)
    {
        var result = await CreateMagicLinkInternalAsync(
            organizationId,
            organizationClientId,
            emailAddress,
            returnUrl);

        return result.IsSuccess
            ? Result.Success(result.Value.Url)
            : Result.Failure<string>(result.Error);
    }

    public async Task<Result<string>> SendMagicLinkWithUrlAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl = null)
    {
        var result = await CreateMagicLinkInternalAsync(
            organizationId,
            organizationClientId,
            emailAddress,
            returnUrl);

        if (!result.IsSuccess)
            return Result.Failure<string>(result.Error);

        await _notifications.SendOrganizationClientPortalMagicLinkAsync(result.Value.Client, result.Value.Url);

        return Result.Success(result.Value.Url);
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

    private async Task<Result<(OrganizationClient Client, string Url)>> CreateMagicLinkInternalAsync(
        Guid organizationId,
        Guid organizationClientId,
        string emailAddress,
        string? returnUrl)
    {
        if (organizationId == Guid.Empty || organizationClientId == Guid.Empty)
            return Result.Failure<(OrganizationClient, string)>(
                Error.Failure("OrganizationClientPortal", "Organization and client are required."));

        if (string.IsNullOrWhiteSpace(emailAddress))
            return Result.Failure<(OrganizationClient, string)>(
                Error.Failure("OrganizationClientPortal", "Email is required."));

        var client = await _clients.Query()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == organizationClientId && x.OrganizationId == organizationId);

        if (client is null)
            return Result.Failure<(OrganizationClient, string)>(
                Error.NotFound("OrganizationClientPortal", "Client not found."));

        if (!string.Equals(client.EmailAddress, emailAddress, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<(OrganizationClient, string)>(
                Error.Failure("OrganizationClientPortal", "Email does not match client record."));

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

        var url = BuildMagicLinkUrl(token, returnUrl);

        return Result.Success((client, url));
    }

    private string BuildMagicLinkUrl(string token, string? returnUrl)
    {
        var url = $"{_frontend.BaseUrl}/client-hub/auth?token={token}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
            url = $"{url}&returnUrl={encodedReturnUrl}";
        }

        return url;
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
