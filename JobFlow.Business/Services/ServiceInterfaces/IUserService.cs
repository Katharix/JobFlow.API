using JobFlow.Domain.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IUserService
{
    Task<Result<IEnumerable<User>>> GetAllUsers();
    Task<Result<User>> GetUserById(Guid userId);
    Task<Result<User>> GetUserByFirebaseUid(string uid);
    Task<Result<User>> UpsertUser(User model);
    Task<Result> DeleteUser(Guid userId);
    Task<Result<User>> GetUserByEmail(string email);
    Task<Result> AssignRole(Guid userId, string role);
    Task<Result<UserProfileDto>> GetProfileByFirebaseUid(string uid);
    Task<Result<UserProfileDto>> UpdateProfile(string uid, UserProfileUpdateRequest request);
}