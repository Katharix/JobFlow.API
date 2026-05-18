using JobFlow.Business.ConfigurationSettings.ConfigurationInterfaces;
using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Utilities;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class EmployeeInviteService : IEmployeeInviteService
{
    private readonly IFrontendSettings _frontendSettings;
    private readonly IRepository<EmployeeInvite> _invites;
    private readonly ILogger<EmployeeInviteService> _logger;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeInviteService(
        ILogger<EmployeeInviteService> logger,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        IFrontendSettings frontendSettings,
        IMapper mapper)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _frontendSettings = frontendSettings;
        _mapper = mapper;
        _invites = unitOfWork.RepositoryOf<EmployeeInvite>();
    }

    public async Task<Result<EmployeeInviteDto>> InviteAsync(EmployeeInvite invite)
    {
        try
        {
            // Validate input
            if (invite.OrganizationId == Guid.Empty)
                return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.OrganizationRequired);

            if (string.IsNullOrWhiteSpace(invite.Email))
                return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.InvalidEmail(invite.Email ?? "unknown"));

            // Revoke any existing pending invite so a fresh one can be created (resend scenario)
            var existingInvite = await _invites.Query()
                .FirstOrDefaultAsync(e => e.Email == invite.Email && e.Status == EmployeeInviteStatus.Pending);

            if (existingInvite is not null)
            {
                existingInvite.Status = EmployeeInviteStatus.Revoked;
                _invites.Update(existingInvite);
            }

            // Create invite
            invite.Id = Guid.NewGuid();
            invite.InviteToken = Guid.NewGuid();
            invite.ExpiresAt = DateTime.UtcNow.AddDays(7);
            invite.ShortCode = ShortCodeGenerator.Generate();
            invite.Status = EmployeeInviteStatus.Pending;

            await _invites.AddAsync(invite);
            await _unitOfWork.SaveChangesAsync();
            var createdInvite = await _invites.Query()
                .Include(i => i.Organization)
                .Include(i => i.Role)
                .Include(i => i.RoleAssignments)
                .FirstOrDefaultAsync(i => i.Id == invite.Id);
            var dto = _mapper.Map<EmployeeInviteDto>(invite);
            if (createdInvite is not null)
                await _notifications.SendEmployeeInviteNotificationAsync(createdInvite);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send EmployeeInvite to {Email}", invite.Email);
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.FailedToSendNotification(invite.Email));
        }
    }

    public async Task<Result<List<EmployeeInviteDto>>> GetByOrganizationAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<List<EmployeeInviteDto>>(EmployeeInviteErrors.OrganizationRequired);

        var invites = await _invites.Query()
            .Include(i => i.Organization)
            .Include(i => i.Role)
            .Include(i => i.RoleAssignments)
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var results = invites.Select(MapInviteDto).ToList();
        return Result.Success(results);
    }

    public async Task<Result> RevokeAsync(Guid inviteId, Guid organizationId)
    {
        if (inviteId == Guid.Empty || organizationId == Guid.Empty)
            return Result.Failure(EmployeeInviteErrors.OrganizationRequired);

        var invite = await _invites.Query()
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.OrganizationId == organizationId);

        if (invite is null)
            return Result.Failure(EmployeeInviteErrors.InviteNotFound);

        if (invite.Status == EmployeeInviteStatus.Revoked)
            return Result.Success();

        invite.Status = EmployeeInviteStatus.Revoked;
        _invites.Update(invite);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<EmployeeDto>> AcceptInviteAsync(Guid inviteToken)
    {
        if (inviteToken == Guid.Empty)
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.NullOrEmptyId);

        var invite = await _invites.Query()
            .Include(i => i.Organization)
            .Include(i => i.Role)
            .Include(i => i.RoleAssignments)
            .FirstOrDefaultAsync(i => i.InviteToken == inviteToken);

        if (invite is null)
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.InviteNotFound);

        if (invite.Status == EmployeeInviteStatus.Revoked)
            return Result.Failure<EmployeeDto>(Error.Failure("EmployeeInvites", "This invitation has been revoked."));

        if (invite.Status == EmployeeInviteStatus.Accepted)
            return Result.Failure<EmployeeDto>(Error.Failure("EmployeeInvites",
                "This invitation has already been accepted."));

        if (invite.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<EmployeeDto>(Error.Failure("EmployeeInvites", "This invitation has expired."));

        // Create new Employee from invite info
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = invite.FirstName ?? string.Empty,
            LastName = invite.LastName ?? string.Empty,
            Email = invite.Email,
            RoleId = invite.RoleId,
            OrganizationId = invite.OrganizationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Resolve full role set from the invite's join rows.
        // The legacy single RoleId remains the primary fallback.
        var roleIds = invite.RoleAssignments.Select(a => a.EmployeeRoleId).Distinct().ToList();
        if (roleIds.Count == 0 && invite.RoleId != Guid.Empty)
            roleIds = new List<Guid> { invite.RoleId };

        foreach (var rid in roleIds)
        {
            employee.RoleAssignments.Add(new EmployeeRoleAssignment
            {
                EmployeeId = employee.Id,
                EmployeeRoleId = rid
            });
        }

        invite.Status = EmployeeInviteStatus.Accepted;

        await _unitOfWork.RepositoryOf<Employee>().AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();


        var employeeDto = _mapper.Map<EmployeeDto>(employee);
        _logger.LogInformation("Employee {Email} accepted invite for Org {OrgId}", employee.Email,
            employee.OrganizationId);

        return Result.Success(employeeDto);
    }

    public async Task<Result<string>> ResolveShortCodeAsync(string code, string? ipAddress = null)
    {
        var invite = await _invites.Query()
            .FirstOrDefaultAsync(i => i.ShortCode == code && i.Status == EmployeeInviteStatus.Pending);

        if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<string>(EmployeeInviteErrors.InviteNotFound);

        invite.AccessCount++;
        invite.AccessedAt = DateTime.UtcNow;
        invite.AccessIpAddress = ipAddress;

        _invites.Update(invite);
        await _unitOfWork.SaveChangesAsync();

        var redirectUrl = $"{_frontendSettings.BaseUrl}/i/{invite.ShortCode}";
        return Result.Success(redirectUrl);
    }

    public async Task<Result<EmployeeInviteDto>> GetInviteByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.InviteNotFound);

        var invite = await _invites.Query()
            .Include(i => i.Organization)
            .Include(i => i.Role)
            .Include(i => i.RoleAssignments)
            .FirstOrDefaultAsync(i => i.ShortCode == code && i.Status == EmployeeInviteStatus.Pending);

        if (invite is null)
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.InviteNotFound);

        var dto = MapInviteDto(invite);
        return Result.Success(dto);
    }

    private EmployeeInviteDto MapInviteDto(EmployeeInvite invite)
    {
        var dto = _mapper.Map<EmployeeInviteDto>(invite);
        dto.OrganizationName = invite.Organization?.OrganizationName;
        dto.RoleName = invite.Role?.Name;
        dto.RoleIds = invite.RoleAssignments?.Select(a => a.EmployeeRoleId).ToList()
                      ?? new List<Guid>();
        dto.IsAccepted = invite.Status == EmployeeInviteStatus.Accepted;
        dto.IsRevoked = invite.Status == EmployeeInviteStatus.Revoked;
        return dto;
    }
}