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
    private readonly IFirebaseUserManager _firebaseUserManager;
    private readonly IFrontendSettings _frontendSettings;
    private readonly IRepository<EmployeeInvite> _invites;
    private readonly ILogger<EmployeeInviteService> _logger;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public EmployeeInviteService(
        ILogger<EmployeeInviteService> logger,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        IFrontendSettings frontendSettings,
        IMapper mapper,
        IUserService userService,
        IFirebaseUserManager firebaseUserManager)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _frontendSettings = frontendSettings;
        _mapper = mapper;
        _userService = userService;
        _firebaseUserManager = firebaseUserManager;
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

    public async Task<Result<EmployeeDto>> AcceptInviteAsync(Guid inviteToken, AcceptInviteRequest request)
    {
        if (inviteToken == Guid.Empty)
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.NullOrEmptyId);

        if (string.IsNullOrWhiteSpace(request.FirebaseUid))
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.FirebaseUidRequired);

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

        var firstName = !string.IsNullOrWhiteSpace(request.FirstName)
            ? request.FirstName!.Trim()
            : invite.FirstName ?? string.Empty;
        var lastName = !string.IsNullOrWhiteSpace(request.LastName)
            ? request.LastName!.Trim()
            : invite.LastName ?? string.Empty;

        // Create the JobFlow User row linked to the Firebase account the
        // invitee just signed up with on the client.
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = invite.Email,
            PhoneNumber = invite.PhoneNumber,
            OrganizationId = invite.OrganizationId,
            FirebaseUid = request.FirebaseUid
        };

        var userResult = await _userService.UpsertUser(user);
        if (userResult.IsFailure)
            return Result.Failure<EmployeeDto>(userResult.Error);

        var roleAssignResult = await _userService.AssignRole(userResult.Value.Id, UserRoles.OrganizationEmployee);
        if (roleAssignResult.IsFailure)
            return Result.Failure<EmployeeDto>(roleAssignResult.Error);

        // Mirror DisplayName + custom claims onto the Firebase user so the
        // existing auth pipeline (FirebaseAuthMiddleware) picks up the role.
        try
        {
            await _firebaseUserManager.SetCustomClaimsAsync(
                request.FirebaseUid,
                UserRoles.OrganizationEmployee,
                invite.OrganizationId);

            await _firebaseUserManager.SetDisplayNameAsync(
                request.FirebaseUid,
                $"{firstName} {lastName}".Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase claim/profile update failed for invite {InviteId}", invite.Id);
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.AccountLinkFailed(ex.Message));
        }

        // Create the Employee record linked to the new User. If an Employee row
        // already exists for this email in the org (e.g. an earlier orphaned
        // accept that never linked a Firebase user), update it in place rather
        // than creating a duplicate.
        var employeeRepo = _unitOfWork.RepositoryOf<Employee>();
        var existingEmployee = await employeeRepo.Query()
            .Include(e => e.RoleAssignments)
            .FirstOrDefaultAsync(e =>
                e.OrganizationId == invite.OrganizationId &&
                e.Email == invite.Email);

        var employee = existingEmployee ?? new Employee
        {
            Id = Guid.NewGuid(),
            OrganizationId = invite.OrganizationId,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.Email = invite.Email;
        employee.PhoneNumber = invite.PhoneNumber;
        employee.RoleId = invite.RoleId;
        employee.UserId = userResult.Value.Id;
        employee.IsActive = true;

        // Resolve full role set from the invite's join rows.
        // The legacy single RoleId remains the primary fallback.
        var roleIds = invite.RoleAssignments.Select(a => a.EmployeeRoleId).Distinct().ToList();
        if (roleIds.Count == 0 && invite.RoleId != Guid.Empty)
            roleIds = new List<Guid> { invite.RoleId };

        employee.RoleAssignments.Clear();
        foreach (var rid in roleIds)
        {
            employee.RoleAssignments.Add(new EmployeeRoleAssignment
            {
                EmployeeId = employee.Id,
                EmployeeRoleId = rid
            });
        }

        invite.Status = EmployeeInviteStatus.Accepted;

        if (existingEmployee is null)
            await employeeRepo.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        var employeeDto = _mapper.Map<EmployeeDto>(employee);
        _logger.LogInformation(
            "Employee {Email} accepted invite for Org {OrgId} (UID={FirebaseUid})",
            employee.Email,
            employee.OrganizationId,
            request.FirebaseUid);

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