using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IUserService
    {
        Task<Result<IEnumerable<User>>> GetAllUsers();
        Task<Result<User>> GetUserById(Guid userId);
        Task<Result<User>> UpsertUser(User model);
        Task<Result> DeleteUser(Guid userId);
        Task<Result<User>> GetUserByEmail(string email);
        Task<Result> AssignRole(Guid userId, string role);
    }
}
