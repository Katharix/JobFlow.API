using FirebaseAdmin.Auth;
using JobFlow.API.Models;
using JobFlow.Business;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [Route("api/organizations/")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private IOrganizationService _organizationService;
        private IOrganizationBrandingService _organizationBrandingService;
        private IPaymentProfileService _paymentProfileService;
        private IUserService _userService;
        private INotificationService _notificationService;

        public OrganizationController(
            IOrganizationService organizationService, 
            IUserService userService,
            IPaymentProfileService paymentProfileService,
            INotificationService notificationService,
            IOrganizationBrandingService organizationBrandingService
           )
        {
            _organizationService = organizationService;
            _userService = userService;
            _paymentProfileService = paymentProfileService;
            _notificationService = notificationService;
            _organizationBrandingService = organizationBrandingService;
        }
        [HttpGet, Route("all")]
        public async Task<IResult> GetAllOrganizations()
        {
            var result = await _organizationService.GetAllOrganizations();
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("create")]
        public async Task<IResult> CreateOrganizationAccount(Organization model)
        {
            var result = await _organizationService.UpsertOrganization(model);
            if (result.IsSuccess)
            {
                await _notificationService.SendOrganizationWelcomeNotificationAsync(model);
            }
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("register")]
        public async Task<IResult> RegisterOrganization(OrganizationRegisterDto model)
        {
            try
            {

                var user = new User
                {
                    Email = model.EmailAddress, 
                    OrganizationId = model.Id.Value,
                    FirebaseUid = model.FireBaseUid
                };

                var userResult = await _userService.UpsertUser(user);
                if (userResult.IsFailure)
                {
                    return userResult.ToProblemDetails();
                }

                await _userService.AssignRole(userResult.Value.Id, model.UserRole);
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(model.FireBaseUid, new Dictionary<string, object>
                {
                    { "role", model.UserRole }
                });
                var orgResults = await _organizationService.GetOrganiztionById(model.Id.Value);
                return orgResults.IsSuccess ? Results.Ok(orgResults.Value) : orgResults.ToProblemDetails();
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"❌ Registration failed: {ex.Message}");
                return Results.Problem($"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost, Route("retrieve")]
        public async Task<IResult> GetOrganizationById([FromBody] OrganizationRequest org)
        { 
            var result = await _organizationService.GetOrganiztionById(org.OrganizationId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        }

        [HttpPost, Route("onboarding")]
        public async Task<IResult> OrganizationOnboarding([FromBody] OnboardingDto onboarding)
        {
            var orgResult = await _organizationService.GetAllOrganizations();
            var ownerId = orgResult.Value.FirstOrDefault(e => e.OrganizationType?.TypeName == "Master Account")?.Id;
            var org = new Organization() 
            { 
                Id = onboarding.OrganizationId,
                DefaultTaxRate = onboarding.DefaultTaxRate,
                EnableTax = onboarding.EnableTax,
                OnBoardingComplete = onboarding.OnboardingComplete
            };
            var branding = new OrganizationBranding()
            {
                LogoUrl = onboarding?.Branding?.LogoUrl,
                FooterNote = onboarding?.Branding?.FooterNote,
                PrimaryColor = onboarding?.Branding?.PrimaryColor,
                SecondaryColor = onboarding?.Branding?.SecondaryColor,
                Tagline = onboarding?.Branding?.Tagline,
                CreatedAt = DateTime.UtcNow,
            };
            var paymentProfile = new CustomerPaymentProfile()
            {
                OwnerType = Domain.Enums.PaymentEntityType.Organization,
                OwnerId = ownerId.Value,
                Provider = onboarding.PaymentProfile.Provider,
                ProviderCustomerId = onboarding.PaymentProfile.ProviderCustomerId,
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
                var result = await _paymentProfileService.CreateAsync(paymentProfile.OwnerId, paymentProfile.OwnerType, paymentProfile.Provider, paymentProfile.ProviderCustomerId);
                if (!result.IsSuccess)
                    return result.ToProblemDetails();
            }
            return Results.Ok();
        }
    }
}
