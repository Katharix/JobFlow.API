using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;


namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService onboarding;

    public OnboardingController(IOnboardingService onboarding)
    {
        this.onboarding = onboarding;
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<IResult> Get(Guid organizationId)
    {
        var result = await onboarding.GetChecklistAsync(organizationId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }
}