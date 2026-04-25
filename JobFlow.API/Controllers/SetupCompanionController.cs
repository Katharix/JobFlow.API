using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}

public sealed record TrackSetupCompanionEventRequest(
    string SessionId,
    string QuestionKey,
    string? AnswerKey);
