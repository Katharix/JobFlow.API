using JobFlow.Business.DI;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class UserService : IUserService
{
    private readonly ILogger<UserService> logger;
    private readonly IUnitOfWork unitOfWork;

    public UserService(ILogger<UserService> logger, IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<User>>> GetAllUsers()
    {
        var userList = await unitOfWork.RepositoryOf<User>().Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();

        if (userList.Count == 0)
            return Result.Failure<IEnumerable<User>>(UserErrors.UserNotFound);

        return Result.Success<IEnumerable<User>>(userList);
    }

    public async Task<Result<User>> GetUserById(Guid userId)
    {
        var user = await unitOfWork.RepositoryOf<User>().Query()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result.Failure<User>(UserErrors.UserNotFound);

        return Result.Success(user);
    }

    public async Task<Result<User>> UpsertUser(User model)
    {
        if (model.Id == Guid.Empty)
            unitOfWork.RepositoryOf<User>().Add(model);
        else
            unitOfWork.RepositoryOf<User>().Update(model);

        await unitOfWork.SaveChangesAsync();
        return Result.Success(model);
    }

    public async Task<Result> DeleteUser(Guid userId)
    {
        var userToDelete = await unitOfWork.RepositoryOf<User>().Query().FirstOrDefaultAsync(u => u.Id == userId);

        if (userToDelete == null)
            return Result.Failure(UserErrors.UserNotFound);

        unitOfWork.RepositoryOf<User>().Remove(userToDelete);
        await unitOfWork.SaveChangesAsync();

        return Result.Success("User successfully deleted.");
    }

    public Task<Result<User>> GetUserByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> AssignRole(Guid userId, string role)
    {
        var identityRole = await unitOfWork.RepositoryOf<SystemRole>().Query().FirstOrDefaultAsync(e => e.Name == role);
        if (identityRole == null) return Result.Failure(Error.NullValue);
        var identityUserRoles = await unitOfWork.RepositoryOf<UserRole>().Query()
            .FirstOrDefaultAsync(e => e.RoleId == identityRole.Id && e.UserId == userId);
        if (identityUserRoles != null) return Result.Failure(UserErrors.UserRoleExist);
        var userRoleToAdd = new UserRole
        {
            UserId = userId,
            RoleId = identityRole.Id
        };
        unitOfWork.RepositoryOf<UserRole>().Add(userRoleToAdd);
        await unitOfWork.SaveChangesAsync();
        return Result.Success("User Role added successfully.");
    }

    public async Task<Result<User>> GetUserByFirebaseUid(string uid)
    {
        var user = await unitOfWork.RepositoryOf<User>()
            .Query()
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.FirebaseUid == uid);

        if (user == null)
            return Result.Failure<User>(UserErrors.UserNotFound);

        var organizationId = user.Organization?.Id;
        var paymentProfileIds = organizationId == null
            ? []
            : await unitOfWork.RepositoryOf<CustomerPaymentProfile>()
                .Query()
                .AsNoTracking()
                .Where(p => p.OwnerId == organizationId)
                .Select(p => p.Id)
                .ToListAsync();
        if (paymentProfileIds.Count > 0)
        {
            var latestSubscription = await unitOfWork.RepositoryOf<SubscriptionRecord>()
                .Query()
                .AsNoTracking()
                .Where(s => paymentProfileIds.Contains(s.PaymentProfileId))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (user.Organization is not null)
                user.Organization.SubscriptionPlanName = latestSubscription?.PlanName;
        }

        return Result.Success(user);
    }

    public async Task<Result<UserProfileDto>> GetProfileByFirebaseUid(string uid)
    {
        var user = await unitOfWork.RepositoryOf<User>()
            .Query()
            .FirstOrDefaultAsync(u => u.FirebaseUid == uid);

        if (user == null)
            return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);

        return Result.Success(ToProfileDto(user));
    }

    public async Task<Result<UserProfileDto>> UpdateProfile(string uid, UserProfileUpdateRequest request)
    {
        var user = await unitOfWork.RepositoryOf<User>()
            .Query()
            .FirstOrDefaultAsync(u => u.FirebaseUid == uid);

        if (user == null)
            return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);

        if (request.FirstName != null)
            user.FirstName = request.FirstName;
        if (request.LastName != null)
            user.LastName = request.LastName;
        if (request.Email != null)
            user.Email = request.Email;
        if (request.PhoneNumber != null)
            user.PhoneNumber = request.PhoneNumber;
        if (request.PreferredLanguage != null)
            user.PreferredLanguage = request.PreferredLanguage;

        unitOfWork.RepositoryOf<User>().Update(user);
        await unitOfWork.SaveChangesAsync();

        return Result.Success(ToProfileDto(user));
    }

    private static UserProfileDto ToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            PreferredLanguage = user.PreferredLanguage
        };
    }


    private static string ResolvePrimaryRole(User user)
    {
        return user.UserRoles
            .Select(ur => ur.Role?.Name)
            .FirstOrDefault() ?? "User";
    }

}