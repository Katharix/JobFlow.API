using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobFlow.API.Controllers
{
    [Route("api/organizations/")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private IOrganizationService _organizationService;
        private IUserService _userService;

        public OrganizationController(
            IOrganizationService organizationService, 
            IUserService userService
           )
        {
            _organizationService = organizationService;
            _userService = userService;
        }
        [HttpGet, Route("all")]
        public async Task<IResult> GetAllOrganizations()
        {
            var result = await _organizationService.GetAllOrganizations();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("register")]
        public async Task<IResult> RegisterOrganization(OrganizationRegisterDto model)
        {
            try
            {
                var org = new Organization
                {
                    OrganizationName = model.OrganizationName,
                    OrganizationTypeId = model.OrganizationTypeId,
                    EmailAddress = model.EmailAddress
                };

                var result = await _organizationService.UpsertOrganizatiom(org);
                if (!result.IsSuccess)
                    return result.ToProblemDetails();

                var user = new User
                {
                    Email = model.EmailAddress,
                    UserName = model.EmailAddress,
                    OrganizationId = org.Id,
                    FirebaseUid = model.FireBaseUid

                };

                var userResult = await _userService.UpsertUser(user);
                if (userResult.IsFailure)
                {
                    return userResult.ToProblemDetails();
                }

                await _userService.AssignRole(userResult.Value.Id, model.UserRole);
                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"❌ Registration failed: {ex.Message}");
                return Results.Problem($"An unexpected error occurred: {ex.Message}");
            }
        }


    }
}
