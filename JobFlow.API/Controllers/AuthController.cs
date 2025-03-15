using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth/")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JobFlowDbContext _dbContext;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        JobFlowDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    [HttpPost, Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        // Check if organization exists, otherwise create it
        var organization = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.OrganizationName == model.OrganizationName);
        if (organization == null)
        {
            organization = new Organization
            {
 
            };
            _dbContext.Organizations.Add(organization);
            await _dbContext.SaveChangesAsync();
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            OrganizationId = organization.Id
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Assign role
        var role = model.IsAdmin ? UserRoles.OrganizationAdmin : UserRoles.OrganizationEmployee;
        await _userManager.AddToRoleAsync(user, role);

        return Ok("User registered successfully!");
    }
}
