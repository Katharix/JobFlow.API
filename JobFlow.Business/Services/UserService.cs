using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services
{
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
            var userList = this.unitOfWork.RepositoryOf<User>().Query();

            if (!userList.Any())
                return Result.Failure<IEnumerable<User>>(UserErrors.UserNotFound);

            return Result.Success<IEnumerable<User>>(userList);
        }

        public async Task<Result<User>> GetUserById(Guid userId)
        {
            var user = await this.unitOfWork.RepositoryOf<User>().Query().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result.Failure<User>(UserErrors.UserNotFound);

            return Result.Success(user);
        }
        public async Task<Result<User>> UpsertUser(User model)
        {
            if (model.Id == Guid.Empty)
                this.unitOfWork.RepositoryOf<User>().Add(model);
            else
                this.unitOfWork.RepositoryOf<User>().Update(model);

            await this.unitOfWork.SaveChangesAsync();
            return Result.Success(model);
        }

        public async Task<Result> DeleteUser(Guid userId)
        {
            var userToDelete = this.unitOfWork.RepositoryOf<User>().Query().FirstOrDefault(u => u.Id == userId);

            if (userToDelete == null)
                return Result.Failure(UserErrors.UserNotFound);

            this.unitOfWork.RepositoryOf<User>().Remove(userToDelete);
            await this.unitOfWork.SaveChangesAsync();

            return Result.Success("User successfully deleted.");
        }

        public Task<Result<User>> GetUserByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> AssignRole(Guid userId, string role)
        {
            var identityRole = this.unitOfWork.RepositoryOf<Role>().Query().FirstOrDefault(e => e.Name == role);
            if (identityRole == null)
            {
                return Result.Failure(Error.NullValue);
            }
            var identityUserRoles = this.unitOfWork.RepositoryOf<UserRole>().Query().FirstOrDefault(e => e.RoleId == identityRole.Id && e.UserId == userId);
            if (identityUserRoles != null)
            {
                return Result.Failure(UserErrors.UserRoleExist);
            }
            var userRoleToAdd = new UserRole()
            {
                UserId = userId,
                RoleId = identityRole.Id
            };
            this.unitOfWork.RepositoryOf<UserRole>().Add(userRoleToAdd);
            await this.unitOfWork.SaveChangesAsync();
            return Result.Success("User Role added successfully.");
        }

        public async Task<Result<User>> GetUserByFirebaseUid(string uid)
        {
            var user = await this.unitOfWork.RepositoryOf<User>().Query().FirstOrDefaultAsync(u => u.FirebaseUid == uid);

            if (user == null)
                return Result.Failure<User>(UserErrors.UserNotFound);

            return Result.Success(user);
        }
    }
}
