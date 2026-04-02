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
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IOrganizationService organizationService,
        IUserService userService,
        IPaymentProfileService paymentProfileService,
        IOrganizationBrandingService organizationBrandingService,
        ILogger<OrganizationController> logger
    )
    {
        _organizationService = organizationService;
        _userService = userService;
        _paymentProfileService = paymentProfileService;
        _organizationBrandingService = organizationBrandingService;
        _logger = logger;
    }

    [HttpGet]
    [Route("all")]
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
                Email = model.EmailAddress,
                OrganizationId = model.Id.Value,
                FirebaseUid = model.FireBaseUid
            };

            var userResult = await _userService.UpsertUser(user);
            if (userResult.IsFailure) return userResult.ToProblemDetails();

            var roleAssignmentResult = await _userService.AssignRole(userResult.Value.Id, model.UserRole);
            if (roleAssignmentResult.IsFailure) return roleAssignmentResult.ToProblemDetails();

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(model.FireBaseUid,
                new Dictionary<string, object>
                {
                    { "role", model.UserRole }
                });
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
}