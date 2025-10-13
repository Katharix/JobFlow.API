using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Business.Utilities;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace JobFlow.Business.Services
{
    [ScopedService]
    public class EmployeeInviteService : IEmployeeInviteService
    {
        private readonly ILogger<EmployeeInviteService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<EmployeeInvite> _invites;
        private readonly INotificationService _notifications;
        private readonly IMapper _mapper;
        private readonly IRepository<Employee> _employees;

        public EmployeeInviteService(
            ILogger<EmployeeInviteService> logger,
            IUnitOfWork unitOfWork,
            INotificationService notifications,
            IMapper mapper)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _notifications = notifications;
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
                    .FirstOrDefaultAsync(e => e.Email == invite.Email && !e.IsAccepted && !e.IsRevoked);

                if (existingInvite is not null)
                    return Result.Failure<EmployeeInviteDto>(EmployeeInviteErrors.AlreadyInvited(invite.Email));

                // Create invite
                invite.Id = Guid.NewGuid();
                invite.InviteToken = TokenGenerator.GenerateInviteToken();
                invite.ExpiresAt = DateTime.UtcNow.AddDays(7);
                invite.IsAccepted = false;
                invite.IsRevoked = false;
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

        public async Task<Result<Employee>> AcceptInviteAsync(string inviteToken)
        {
            if (string.IsNullOrWhiteSpace(inviteToken))
                return Result.Failure<Employee>(EmployeeInviteErrors.NullOrEmptyId);

            var invite = await _invites.Query()
                .Include(i => i.Organization)
                .Include(i => i.Role)
                .FirstOrDefaultAsync(i => i.InviteToken == inviteToken);

            if (invite is null)
                return Result.Failure<Employee>(EmployeeInviteErrors.InviteNotFound);

            if (invite.IsRevoked)
                return Result.Failure<Employee>(Error.Failure("EmployeeInvites", "This invitation has been revoked."));

            if (invite.IsAccepted)
                return Result.Failure<Employee>(Error.Failure("EmployeeInvites", "This invitation has already been accepted."));

            if (invite.ExpiresAt < DateTime.UtcNow)
                return Result.Failure<Employee>(Error.Failure("EmployeeInvites", "This invitation has expired."));

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

            invite.IsAccepted = true;

            await _employees.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Employee {Email} accepted invite for Org {OrgId}", employee.Email, employee.OrganizationId);

            return Result.Success(employee);
        }

        public async Task<Result<string>> ResolveShortCodeAsync(string code, string? ipAddress = null)
        {
            var invite = await _invites.Query()
                .FirstOrDefaultAsync(i => i.ShortCode == code && !i.IsRevoked && !i.IsAccepted);

            if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
                return Result.Failure<string>(EmployeeInviteErrors.InviteNotFound);

            invite.AccessCount++;
            invite.AccessedAt = DateTime.UtcNow;
            invite.AccessIpAddress = ipAddress;

            _invites.Update(invite);
            await _unitOfWork.SaveChangesAsync();

            var redirectUrl = $"https://jobflow.katharix.com/invite/{invite.InviteToken}";
            return Result.Success(redirectUrl);
        }

    }
}
