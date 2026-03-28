using FirebaseAdmin.Auth;
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

    public OrganizationController(
        IOrganizationService organizationService,
        IUserService userService,
        IPaymentProfileService paymentProfileService,
        IOrganizationBrandingService organizationBrandingService
    )
    {
        _organizationService = organizationService;
        _userService = userService;
        _paymentProfileService = paymentProfileService;
        _organizationBrandingService = organizationBrandingService;
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

            await _userService.AssignRole(userResult.Value.Id, model.UserRole);
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(model.FireBaseUid,
                new Dictionary<string, object>
                {
                    { "role", model.UserRole }
                });
            var orgResults = await _organizationService.GetOrganizationDtoById(model.Id.Value);
            return orgResults.IsSuccess ? Results.Ok(orgResults.Value) : orgResults.ToProblemDetails();
        }
        catch (Exception ex)
        {
            // Log error if needed
            Console.WriteLine($"❌ Registration failed: {ex.Message}");
            return Results.Problem($"An unexpected error occurred: {ex.Message}");
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