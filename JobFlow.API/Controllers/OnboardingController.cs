using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService onboarding;
    private readonly IOrganizationService organizations;

    public OnboardingController(IOnboardingService onboarding, IOrganizationService organizations)
    {
        this.onboarding = onboarding;
        this.organizations = organizations;
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<IResult> Get(Guid organizationId)
    {
        var result = await onboarding.GetChecklistAsync(organizationId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpGet("quick-start")]
    public async Task<IResult> GetQuickStartState()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await onboarding.GetQuickStartStateAsync(organizationId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("quick-start")]
    public async Task<IResult> ApplyQuickStart([FromBody] OnboardingQuickStartApplyRequestDto request)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var orgResult = await organizations.GetOrganizationDtoById(organizationId);
        if (orgResult.IsFailure)
        {
            return orgResult.ToProblemDetails();
        }

        if (!HasMinPlan(orgResult.Value.SubscriptionPlanName, "Flow"))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Subscription Required",
                detail: "A Flow plan is required to apply quick-start presets.");
        }

        var result = await onboarding.ApplyQuickStartAsync(organizationId, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("complete")]
    public async Task<IResult> CompleteOnboarding()
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await onboarding.MarkOrganizationCompleteIfEligibleAsync(organizationId);
        if (!result.IsSuccess)
            return result.ToProblemDetails();

        if (!result.Value)
        {
            return Results.Conflict(new
            {
                completed = false,
                message = "Onboarding checklist is not complete yet."
            });
        }

        var orgResult = await organizations.GetOrganizationDtoById(organizationId);
        return orgResult.IsSuccess
            ? Results.Ok(orgResult.Value)
            : orgResult.ToProblemDetails();
    }

    private static bool HasMinPlan(string? planName, string required)
    {
        static int Rank(string? plan)
        {
            var value = (plan ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "go" => 0,
                "flow" => 1,
                "max" => 2,
                _ => -1
            };
        }

        return Rank(planName) >= Rank(required);
    }
}