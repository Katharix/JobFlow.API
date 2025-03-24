using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> logger;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRepository<User> users;
        private readonly IRepository<IdentityUserRole<Guid>> userRoles;
        private readonly IRepository<IdentityRole<Guid>> roles;

        public UserService(ILogger<UserService> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            this.users = this.unitOfWork.RepositoryOf<User>();
            this.roles = this.unitOfWork.RepositoryOf<IdentityRole<Guid>>();
            this.userRoles = this.unitOfWork.RepositoryOf<IdentityUserRole<Guid>>();
        }

        public async Task<Result<IEnumerable<User>>> GetAllUsers()
        {
            var userList = this.users.Query();

            if (!userList.Any())
                return Result.Failure<IEnumerable<User>>(UserErrors.UserNotFound);

            return Result.Success<IEnumerable<User>>(userList);
        }

        public async Task<Result<User>> GetUserById(Guid userId)
        {
            var user = await this.users.Query().FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result.Failure<User>(UserErrors.UserNotFound);

            return Result.Success(user);
        }
        public async Task<Result<User>> UpsertUser(User model)
        {
            if (model.Id == Guid.Empty)
                this.users.Add(model);
            else
                this.users.Update(model);

            await this.unitOfWork.SaveChangesAsync();
            return Result.Success(model);
        }

        public async Task<Result> DeleteUser(Guid userId)
        {
            var userToDelete = this.users.Query().FirstOrDefault(u => u.Id == userId);

            if (userToDelete == null)
                return Result.Failure(UserErrors.UserNotFound);

            users.Remove(userToDelete);
            await this.unitOfWork.SaveChangesAsync();

            return Result.Success("User successfully deleted.");
        }

        public Task<Result<User>> GetUserByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> AssignRole(Guid userId, string role)
        {
            var identityRole = this.roles.Query().FirstOrDefault(e => e.Name == role);
            if (identityRole == null)
            {
                return Result.Failure(Error.NullValue);
            }
            var identityUserRoles = this.userRoles.Query().FirstOrDefault(e => e.RoleId == identityRole.Id && e.UserId == userId);
            if (identityUserRoles != null)
            {
                return Result.Failure(UserErrors.UserRoleExist);
            }
            var userRoleToAdd = new IdentityUserRole<Guid>()
            {
                UserId = userId,
                RoleId = identityRole.Id
            };
            this.userRoles.Add(userRoleToAdd);
            await this.unitOfWork.SaveChangesAsync();
            return Result.Success("User Role added successfully.");
        }
    }
}
