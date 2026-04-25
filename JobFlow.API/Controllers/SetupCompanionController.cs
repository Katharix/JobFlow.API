using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/setup-companion")]
[Authorize]
public class SetupCompanionController : ControllerBase
{
    private readonly ISetupCompanionService _setupCompanionService;

    public SetupCompanionController(ISetupCompanionService setupCompanionService)
    {
        _setupCompanionService = setupCompanionService;
    }

    [HttpPost("events")]
    public async Task<IResult> TrackEvent([FromBody] TrackSetupCompanionEventRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _setupCompanionService.TrackEventAsync(
            organizationId,
            request.SessionId,
            request.QuestionKey,
            request.AnswerKey);

        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    [HttpPost("ask")]
    [EnableRateLimiting("companion-ask")]
    public async Task<IResult> Ask([FromBody] AskSetupCompanionRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _setupCompanionService.AskAsync(
            organizationId,
            request.SessionId,
            request.Question,
            request.CurrentRoute);

        return result.IsSuccess
            ? Results.Ok(new { answer = result.Value })
            : result.ToProblemDetails();
    }
}

public sealed record TrackSetupCompanionEventRequest(
    string SessionId,
    string QuestionKey,
    string? AnswerKey);

public sealed record AskSetupCompanionRequest(
    string SessionId,
    string Question,
    string CurrentRoute);
