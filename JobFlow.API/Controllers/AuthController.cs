using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FirebaseAdmin.Auth;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/auth/")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public AuthController(
        IUserService userService,
        IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    // ============================================================
    // LOGIN WITH FIREBASE
    // ============================================================
    [HttpPost]
    [Route("login-with-firebase")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithFirebase([FromBody] TokenDto model)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(model.Token);
            var firebaseUid = decodedToken.Uid;
            var email = decodedToken.Claims.ContainsKey("email")
                ? decodedToken.Claims["email"]?.ToString()
                : null;

            var userInfo = await _userService.GetUserByFirebaseUid(firebaseUid);
            User user;

            if (userInfo.Error == UserErrors.UserNotFound && !string.IsNullOrEmpty(email))
            {
                user = new User
                {
                    FirebaseUid = firebaseUid,
                    Email = email,
                    CreatedAt = DateTime.Now
                };

                await _userService.UpsertUser(user);
                await _userService.AssignRole(user.Id, "User");
            }
            else
                user = userInfo.Value;

            return Ok(new { user.Organization });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { Message = "Invalid Firebase token.", Error = ex.Message });
        }
    }

    // ============================================================
    // CREATE FIREBASE ACCOUNT (SUPER ADMIN)
    // ============================================================
    [HttpPost("create-account")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateFirebaseAccount([FromBody] CreateAccountRequest model)
    {
        try
        {
            var args = new UserRecordArgs
            {
                Email = model.Email,
                Password = model.Password,
                DisplayName = model.DisplayName,
                EmailVerified = false,
                Disabled = false
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

            // Optionally create JobFlow DB record too
            var newUser = new User
            {
                FirebaseUid = userRecord.Uid,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow
            };

            await _userService.UpsertUser(newUser);
            await _userService.AssignRole(newUser.Id, model.Role ?? "OrganizationEmployee");

            return Ok(new
            {
                Message = "Firebase account created successfully.",
                userRecord.Uid,
                userRecord.Email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to create Firebase account.", Error = ex.Message });
        }
    }

    // ============================================================
    // SEND PASSWORD RESET LINK
    // ============================================================
    [HttpPost("password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetRequest model)
    {
        try
        {
            var link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(model.Email);
            return Ok(new { ResetLink = link });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to generate reset link.", Error = ex.Message });
        }
    }

    // ============================================================
    // LOOKUP ACCOUNT BY EMAIL OR UID
    // ============================================================
    [HttpGet("lookup")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Lookup([FromQuery] string email = "", [FromQuery] string uid = "")
    {
        try
        {
            UserRecord userRecord;

            if (!string.IsNullOrEmpty(email))
                userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            else if (!string.IsNullOrEmpty(uid))
                userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            else
                return BadRequest("Provide either 'email' or 'uid'.");

            return Ok(new
            {
                userRecord.Uid,
                userRecord.Email,
                userRecord.DisplayName,
                userRecord.EmailVerified,
                userRecord.Disabled,
                userRecord.CustomClaims
            });
        }
        catch (Exception ex)
        {
            return NotFound(new { Message = "Account not found.", Error = ex.Message });
        }
    }

    // ============================================================
    // UPDATE FIREBASE ACCOUNT (SUPER ADMIN)
    // ============================================================
    [HttpPut("update/{uid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateAccount(string uid, [FromBody] UpdateAccountRequest model)
    {
        try
        {
            var args = new UserRecordArgs
            {
                Uid = uid,
                DisplayName = model.DisplayName,
                Disabled = model.Disabled
            };

            var updatedUser = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

            return Ok(new
            {
                Message = "User updated successfully.",
                updatedUser.Uid,
                updatedUser.DisplayName,
                updatedUser.Disabled
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to update user.", Error = ex.Message });
        }
    }

    // ============================================================
    // DELETE FIREBASE ACCOUNT (SUPER ADMIN)
    // ============================================================
    [HttpDelete("delete/{uid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteAccount(string uid)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            return Ok(new { Message = "Firebase user deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to delete user.", Error = ex.Message });
        }
    }

}

// ============================================================
// DTOs
// ============================================================
public class TokenDto
{
    public string Token { get; set; }
}

public class CreateAccountRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Role { get; set; }
}

public class PasswordResetRequest
{
    public string Email { get; set; } = default!;
}

public class UpdateAccountRequest
{
    public string DisplayName { get; set; } = default!;
    public bool Disabled { get; set; }
}