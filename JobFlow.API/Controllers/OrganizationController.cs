using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2.Responses;
using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[Route("api/organizations/")]
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationBrandingService _organizationBrandingService;
    private readonly IOrganizationService _organizationService;
    private readonly IPaymentProfileService _paymentProfileService;
    private readonly IUserService _userService;
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeRoleService _employeeRoleService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IOrganizationService organizationService,
        IUserService userService,
        IPaymentProfileService paymentProfileService,
        IOrganizationBrandingService organizationBrandingService,
        IEmployeeService employeeService,
        IEmployeeRoleService employeeRoleService,
        ILogger<OrganizationController> logger
    )
    {
        _organizationService = organizationService;
        _userService = userService;
        _paymentProfileService = paymentProfileService;
        _organizationBrandingService = organizationBrandingService;
        _employeeService = employeeService;
        _employeeRoleService = employeeRoleService;
        _logger = logger;
    }

    [HttpGet]
    [Route("all")]
    [Authorize]
    public async Task<IResult> GetAllOrganizations()
    {
        var result = await _organizationService.GetAllOrganizations();
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("create")]
    [AllowAnonymous]
    public async Task<IResult> CreateOrganizationAccount(Organization model)
    {
        var result = await _organizationService.UpsertOrganization(model);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut]
    [Route("update")]
    [Authorize(Policy = "OrganizationAdminOnly")]
    public async Task<IResult> UpdateOrganization([FromBody] UpdateOrganizationRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _organizationService.UpdateOrganizationAsync(organizationId, request);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Route("register")]
    public async Task<IResult> RegisterOrganization(OrganizationRegisterDto model)
    {
        try
        {
            if (!model.Id.HasValue)
                return Results.BadRequest("Organization id is required.");

            if (string.IsNullOrWhiteSpace(model.UserRole))
                return Results.BadRequest("User role is required.");

            if (string.IsNullOrWhiteSpace(model.FireBaseUid))
                return Results.BadRequest("Firebase uid is required.");

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.EmailAddress,
                OrganizationId = model.Id.Value,
                FirebaseUid = model.FireBaseUid
            };

            var userResult = await _userService.UpsertUser(user);
            if (userResult.IsFailure) return userResult.ToProblemDetails();

            // Assign both OrganizationAdmin and OrganizationEmployee roles
            var adminRoleResult = await _userService.AssignRole(userResult.Value.Id, UserRoles.OrganizationAdmin);
            if (adminRoleResult.IsFailure) return adminRoleResult.ToProblemDetails();

            var employeeRoleResult = await _userService.AssignRole(userResult.Value.Id, UserRoles.OrganizationEmployee);
            if (employeeRoleResult.IsFailure) return employeeRoleResult.ToProblemDetails();

            // Set Firebase custom claims and displayName
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(model.FireBaseUid,
                new Dictionary<string, object>
                {
                    { "role", UserRoles.OrganizationAdmin }
                });

            var displayName = $"{model.FirstName} {model.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                {
                    Uid = model.FireBaseUid,
                    DisplayName = displayName
                });
            }

            // Auto-create an Employee record linked to the new user
            await CreateOwnerEmployeeAsync(model, userResult.Value);

            // Apply org size from registration payload
            if (!string.IsNullOrWhiteSpace(model.OrgSize))
                await _organizationService.SetOrgSizeAsync(model.Id.Value, model.OrgSize);

            var orgResults = await _organizationService.GetOrganizationDtoById(model.Id.Value);
            return orgResults.IsSuccess ? Results.Ok(orgResults.Value) : orgResults.ToProblemDetails();
        }
        catch (TokenResponseException ex)
        {
            if (!string.IsNullOrWhiteSpace(ex.Error?.Error)
                && ex.Error.Error.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogCritical(ex,
                    "Organization registration failed due to invalid Firebase service-account JWT signature. OrgId={OrganizationId}, FirebaseUid={FirebaseUid}",
                    model.Id,
                    model.FireBaseUid);

                return Results.Problem(
                    title: "Registration service temporarily unavailable.",
                    detail: "Identity provider configuration is currently unavailable. Please try again shortly.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            _logger.LogError(ex,
                "Organization registration failed while requesting Firebase access token. OrgId={OrganizationId}, FirebaseUid={FirebaseUid}",
                model.Id,
                model.FireBaseUid);

            return Results.Problem(
                title: "Registration service temporarily unavailable.",
                detail: "Unable to reach identity provider services right now. Please try again in a moment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (FirebaseAuthException ex)
        {
            if (!string.IsNullOrWhiteSpace(ex.Message)
                && ex.Message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogCritical(ex,
                    "Organization registration failed due to invalid Firebase service-account credentials. OrgId={OrganizationId}, FirebaseUid={FirebaseUid}",
                    model.Id,
                    model.FireBaseUid);

                return Results.Problem(
                    title: "Registration service temporarily unavailable.",
                    detail: "Identity provider configuration is currently unavailable. Please try again shortly.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            _logger.LogError(ex,
                "Organization registration failed while assigning Firebase custom claims. OrgId={OrganizationId}, FirebaseUid={FirebaseUid}",
                model.Id,
                model.FireBaseUid);

            return Results.Problem(
                title: "Registration service temporarily unavailable.",
                detail: "Unable to finalize account provisioning right now. Please try again in a moment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Organization registration failed unexpectedly. OrgId={OrganizationId}, FirebaseUid={FirebaseUid}",
                model.Id,
                model.FireBaseUid);

            return Results.Problem(
                title: "An error occurred while processing your request.",
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    [Route("retrieve")]
    [AllowAnonymous]
    public async Task<IResult> GetOrganizationById([FromBody] OrganizationRequest org)
    {
        var result = await _organizationService.GetOrganizationDtoById(org.OrganizationId);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut]
    [Route("industry")]
    public async Task<IResult> UpdateIndustry([FromBody] OrganizationIndustryUpdateDto request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _organizationService.UpdateIndustryAsync(organizationId, request.IndustryKey);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }

        var dtoResult = await _organizationService.GetOrganizationDtoById(organizationId);
        return dtoResult.IsSuccess ? Results.Ok(dtoResult.Value) : dtoResult.ToProblemDetails();
    }

    [HttpPost]
    [Route("onboarding")]
    public async Task<IResult> OrganizationOnboarding([FromBody] OnboardingDto onboarding)
    {
        var orgResult = await _organizationService.GetAllOrganizations();
        var ownerId = orgResult.Value.FirstOrDefault(e => e.OrganizationType?.TypeName == "Master Account")?.Id;
        if (!ownerId.HasValue)
            return Results.Problem("Master account not found.");

        var paymentProfileDto = onboarding.PaymentProfile;
        if (paymentProfileDto is null)
            return Results.Problem("Payment profile is required.");
        var org = new Organization
        {
            Id = onboarding.OrganizationId,
            DefaultTaxRate = onboarding.DefaultTaxRate,
            EnableTax = onboarding.EnableTax,
            OnBoardingComplete = onboarding.OnboardingComplete
        };
        OrganizationBranding? branding = null;
        if (onboarding.Branding is not null)
        {
            branding = new OrganizationBranding
            {
                LogoUrl = onboarding.Branding.LogoUrl,
                FooterNote = onboarding.Branding.FooterNote,
                PrimaryColor = onboarding.Branding.PrimaryColor,
                SecondaryColor = onboarding.Branding.SecondaryColor,
                Tagline = onboarding.Branding.Tagline,
                CreatedAt = DateTime.UtcNow
            };
        }
        var paymentProfile = new CustomerPaymentProfile
        {
            OwnerType = PaymentEntityType.Organization,
            OwnerId = ownerId.Value,
            Provider = paymentProfileDto.Provider,
            ProviderCustomerId = paymentProfileDto.ProviderCustomerId,
            CreatedAt = DateTime.UtcNow
        };

        if (org != null)
        {
            var result = await _organizationService.UpsertOrganization(org);
            if (!result.IsSuccess)
                return result.ToProblemDetails();
        }

        if (branding != null)
        {
            var result = await _organizationBrandingService.CreateOrUpdateAsync(branding);
            if (!result.IsSuccess)
                return result.ToProblemDetails();
        }

        if (paymentProfile != null)
        {
            var result = await _paymentProfileService.CreateAsync(paymentProfile.OwnerId, paymentProfile.OwnerType,
                paymentProfile.Provider, paymentProfile.ProviderCustomerId);
            if (!result.IsSuccess)
                return result.ToProblemDetails();
        }

        return Results.Ok();
    }

    private async Task CreateOwnerEmployeeAsync(OrganizationRegisterDto model, User user)
    {
        try
        {
            var orgId = model.Id!.Value;

            // Ensure at least one employee role exists; create a default "Owner" role if none
            var rolesResult = await _employeeRoleService.GetRolesByOrganizationAsync(orgId);
            Guid employeeRoleId;
            if (rolesResult.IsSuccess && rolesResult.Value.Any())
            {
                employeeRoleId = rolesResult.Value.First().Id;
            }
            else
            {
                var ownerRole = new EmployeeRole
                {
                    Id = Guid.NewGuid(),
                    Name = "Owner",
                    Description = "Organization owner",
                    OrganizationId = orgId
                };
                var roleResult = await _employeeRoleService.UpsertAsync(ownerRole);
                employeeRoleId = roleResult.IsSuccess ? roleResult.Value.Id : ownerRole.Id;
            }

            var employeeRequest = new CreateEmployeeRequest
            {
                OrganizationId = orgId,
                UserId = user.Id,
                FirstName = model.FirstName ?? string.Empty,
                LastName = model.LastName ?? string.Empty,
                Email = model.EmailAddress,
                RoleId = employeeRoleId
            };

            await _employeeService.CreateAsync(employeeRequest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to auto-create employee record during registration. OrgId={OrganizationId}, UserId={UserId}",
                model.Id, user.Id);
        }
    }
}