using JobFlow.Business.ExternalServices.Twilio;
using JobFlow.Business.ExternalServices.Twilio.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobFlow.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize] // Requires authentication
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JobFlowDbContext _dbContext;
        private readonly ITwilioService _twilioService;

        public UserController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            JobFlowDbContext dbContext,
            ITwilioService twilioService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _twilioService = twilioService;
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Super Admin, Organization Admin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .Include(u => u.Organization)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.OrganizationId,
                    Organization = u.Organization.OrganizationName,
                    Roles = _userManager.GetRolesAsync(u).Result
                })
                .ToListAsync();
            var user = users.FirstOrDefault();
            var twilioModel = new TwilioModel
            {
                Message = $"All Job Flow users were retrieved. {user.UserName}!",
                RecipientPhoneNumber = "540-642-9153"
            };
            await this._twilioService.SendTextMessage(twilioModel);
            return Ok(users);
        }

        /// <summary>
        /// Get a single user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.OrganizationId,
                Roles = roles
            });
        }

        /// <summary>
        /// Update user role (Admin only)
        /// </summary>
        [HttpPost("{id}/update-role")]
        [Authorize(Roles = "Super Admin, Organization Admin")]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateRoleDto model)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("User not found");

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Assign new role
            var roleExists = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExists) return BadRequest("Invalid role");

            await _userManager.AddToRoleAsync(user, model.Role);
            return Ok($"User role updated to {model.Role}");
        }

        /// <summary>
        /// Delete a user (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin, Organization Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("User not found");

            await _userManager.DeleteAsync(user);
            return Ok("User deleted successfully.");
        }

        /// <summary>
        /// Invite a user to an organization (Admin only)
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Roles = "Super Admin, Organization Admin")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto model)
        {
            var admin = await _userManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email));
            if (admin == null) return Unauthorized();

            var user = new User
            {
                Email = model.Email,
                UserName = model.Email,
                OrganizationId = admin.OrganizationId, // Assign same organization as the admin
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Assign role
            await _userManager.AddToRoleAsync(user, model.Role);

            // Send invitation email (to be implemented)
            return Ok("User invited successfully. They must set their password to activate their account.");
        }
    }
}
