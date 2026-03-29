using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class SupportHubInviteService : ISupportHubInviteService
{
    private const int MaxCodeAttempts = 5;
    private readonly IRepository<SupportHubInvite> _invites;
    private readonly IUnitOfWork _unitOfWork;

    public SupportHubInviteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _invites = unitOfWork.RepositoryOf<SupportHubInvite>();
    }

    public async Task<Result<SupportHubInviteDto>> CreateInviteAsync(
        SupportHubInviteCreateRequest request,
        string? createdBy)
    {
        var code = await GenerateUniqueCodeAsync();
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<SupportHubInviteDto>(
                Error.Failure("SupportHub.InviteGenerationFailed", "Unable to generate invite code."));
        }

        var invite = new SupportHubInvite
        {
            Id = Guid.NewGuid(),
            Code = code,
            Role = request.Role,
            CreatedBy = createdBy,
            ExpiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
        };

        await _invites.AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToDto(invite));
    }

    public async Task<Result<List<SupportHubInviteDto>>> GetActiveInvitesAsync()
    {
        var invites = await _invites.Query()
            .AsNoTracking()
            .Where(x => x.RedeemedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var results = invites.Select(ToDto).ToList();
        return Result.Success(results);
    }

    public async Task<Result<SupportHubInviteValidationDto>> ValidateInviteAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Success(new SupportHubInviteValidationDto(null, "Invite code is required."));
        }

        var normalized = code.Trim().ToUpperInvariant();
        var invite = await _invites.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized);

        if (invite is null)
        {
            return Result.Success(new SupportHubInviteValidationDto(null, "Invite code not found."));
        }

        if (invite.RedeemedAt.HasValue)
        {
            return Result.Success(new SupportHubInviteValidationDto(null, "Invite code already redeemed."));
        }

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Result.Success(new SupportHubInviteValidationDto(null, "Invite code expired."));
        }

        return Result.Success(new SupportHubInviteValidationDto(ToDto(invite), null));
    }

    public async Task<Result<SupportHubInviteDto>> RedeemInviteAsync(string code, string? redeemedBy)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<SupportHubInviteDto>(
                Error.Validation("SupportHub.InviteRequired", "Invite code is required."));
        }

        var normalized = code.Trim().ToUpperInvariant();
        var invite = await _invites.Query().FirstOrDefaultAsync(x => x.Code == normalized);
        if (invite is null)
        {
            return Result.Failure<SupportHubInviteDto>(
                Error.NotFound("SupportHub.InviteNotFound", "Invite code not found."));
        }

        if (invite.RedeemedAt.HasValue)
        {
            return Result.Failure<SupportHubInviteDto>(
                Error.Conflict("SupportHub.InviteAlreadyRedeemed", "Invite code already redeemed."));
        }

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Result.Failure<SupportHubInviteDto>(
                Error.Validation("SupportHub.InviteExpired", "Invite code expired."));
        }

        invite.RedeemedAt = DateTimeOffset.UtcNow;
        invite.RedeemedByUid = redeemedBy;

        _invites.Update(invite);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success(ToDto(invite));
    }

    private async Task<string?> GenerateUniqueCodeAsync()
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt += 1)
        {
            var code = GenerateCode();
            var exists = await _invites.ExistsAsync(x => x.Code == code);
            if (!exists)
            {
                return code;
            }
        }

        return null;
    }

    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[8];
        var random = new Random();
        for (var i = 0; i < chars.Length; i += 1)
        {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    private static SupportHubInviteDto ToDto(SupportHubInvite invite)
    {
        return new SupportHubInviteDto(
            invite.Id,
            invite.Code,
            invite.Role,
            invite.CreatedAt,
            invite.CreatedBy,
            invite.ExpiresAt,
            invite.RedeemedAt,
            invite.RedeemedByUid);
    }
}
