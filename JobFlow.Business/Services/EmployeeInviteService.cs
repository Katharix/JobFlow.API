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
    private readonly IRepository<Employee> _employees;
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

            // Check for existing active invite
            var existingInvite = await _invites.Query()
                .FirstOrDefaultAsync(e => e.Email == invite.Email && invite.Status == EmployeeInviteStatus.Pending);

            if (existingInvite is not null)
                return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.AlreadyInvited(invite.Email));

            // Create invite
            invite.Id = Guid.NewGuid();
            invite.InviteToken = Guid.NewGuid();
            invite.ExpiresAt = DateTime.UtcNow.AddDays(7);
            invite.ShortCode = ShortCodeGenerator.Generate();

            await _invites.AddAsync(invite);
            await _unitOfWork.SaveChangesAsync();

            var createdInvite = await _invites.Query()
                .Include(i => i.Organization)
                .Include(i => i.Role)
                .FirstOrDefaultAsync(i => i.Id == invite.Id);
            var dto = _mapper.Map<EmployeeInviteDto>(invite);
            await _notifications.SendEmployeeInviteNotificationAsync(createdInvite);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send EmployeeInvite to {Email}", invite.Email);
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.FailedToSendNotification(invite.Email));
        }
    }

    public async Task<Result<EmployeeDto>> AcceptInviteAsync(Guid inviteToken)
    {
        if (inviteToken == Guid.Empty)
            return Result.Failure<EmployeeDto>(EmployeeInviteErrors.NullOrEmptyId);

        var invite = await _invites.Query()
            .Include(i => i.Organization)
            .Include(i => i.Role)
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
            FirstName = invite.FirstName,
            LastName = invite.LastName,
            Email = invite.Email,
            RoleId = invite.RoleId,
            OrganizationId = invite.OrganizationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

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

        var redirectUrl = $"{_frontendSettings.BaseUrl}/invite/{invite.InviteToken}";
        return Result.Success(redirectUrl);
    }

    public async Task<Result<EmployeeInviteDto>> GetInviteByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.InviteNotFound);

        var invite = await _invites.Query()
            .Include(i => i.Organization)
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.ShortCode == code && i.Status == EmployeeInviteStatus.Pending);

        if (invite is null)
            return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.InviteNotFound);

        var dto = _mapper.Map<EmployeeInviteDto>(invite);
        return Result.Success(dto);
    }
}