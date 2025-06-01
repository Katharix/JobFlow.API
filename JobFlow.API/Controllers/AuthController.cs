using FirebaseAdmin.Auth;
using JobFlow.Business.ModelErrors;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/auth/")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserService userService,
        JobFlowDbContext dbContext,
        IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost, Route("login-with-firebase")]
    public async Task<IActionResult> LoginWithFirebase([FromBody] TokenDto model)
    {
        //To do: The user must be associated with an Organization
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(model.Token);
            var firebaseUid = decodedToken.Uid;
            var email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : null;
            var user = new User();
            var userInfo = await _userService.GetUserByFirebaseUid(firebaseUid);
            if (userInfo.Error == UserErrors.UserNotFound && !String.IsNullOrEmpty(email))
            {
                 user = new User
                {
                    FirebaseUid = firebaseUid,
                    Email = email,
                    CreatedAt = DateTime.Now
                };
                await _userService.UpsertUser(user);

                // ✅ Assign default role (e.g., "User")
                await _userService.AssignRole(user.Id, "User");
            }
            else
            {
                user = userInfo.Value;
            }
                //var authClaims = new List<Claim>
                //    {
                //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                //        new Claim(ClaimTypes.Name, user.Email),
                //        new Claim("OrganizationId", user.OrganizationId.ToString())
                //    };

                //authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

                //var token = GenerateJwtToken(authClaims);
                return Ok(new { Organization = user.Organization });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { Message = "Invalid Firebase token.", Error = ex.Message });
        }
    }

    /// <summary>
    /// Register a new user (Admin or Employee)
    /// </summary>
    //[HttpPost, Route("register")]
    //[AllowAnonymous]
    //public async Task<IActionResult> Register([FromBody] RegisterDto model)
    //{
    //    // Check if user already exists
    //    var existingUser = await _userManager.FindByEmailAsync(model.Email);
    //    if (existingUser != null)
    //        return BadRequest("User already exists.");

    //    // Create new user
    //    var user = new User
    //    {
    //        UserName = model.Email,
    //        Email = model.Email,
    //        OrganizationId = model.OrganizationId
    //    };

    //    var result = await _userManager.CreateAsync(user, model.Password);
    //    if (!result.Succeeded)
    //        return BadRequest(result.Errors);

    //    // Assign role
    //    var roleExists = await _roleManager.RoleExistsAsync(model.Role);
    //    if (!roleExists)
    //        return BadRequest("Invalid role specified.");

    //    await _userManager.AddToRoleAsync(user, model.Role);

    //    return Ok("User registered successfully!");
    //}

    /// <summary>
    /// User login endpoint - Returns JWT token
    /// </summary>
    //[HttpPost("login")]
    //public async Task<IActionResult> Login([FromBody] LoginDto model)
    //{
    //    var user = await _userManager.FindByEmailAsync(model.Email);
    //    if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
    //        return Unauthorized("Invalid credentials");

    //    var userRoles = await _userManager.GetRolesAsync(user);
    //    var authClaims = new List<Claim>
    //    {
    //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    //        new Claim(ClaimTypes.Name, user.Email),
    //        new Claim("OrganizationId", user.OrganizationId.ToString())
    //    };

    //    authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

    //    var token = GenerateJwtToken(authClaims);
    //    return Ok(new { token, role = userRoles.FirstOrDefault(), organizationId = user.OrganizationId });
    //}

    private string GenerateJwtToken(List<Claim> claims)
    {
        var jwtKey = _configuration.GetSection("JWTKey").Value ?? throw new Exception("JWT Key is missing.");
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(12),
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateJwtToken(User user, IList<string> roles)
    {
        var jwtKey = _configuration.GetSection("JWTKey").Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("FirebaseUid", user.FirebaseUid),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // ✅ Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
public class TokenDto
{
    public string Token { get; set; }
}
