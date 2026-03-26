using JobFlow.API.Extensions;
using JobFlow.Business.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/follow-up-automation")]
[Authorize]
public class FollowUpAutomationController : ControllerBase
{
    private readonly IFollowUpAutomationService _followUpAutomation;

    public FollowUpAutomationController(IFollowUpAutomationService followUpAutomation)
    {
        _followUpAutomation = followUpAutomation;
    }

    [HttpGet("sequences")]
    public async Task<IActionResult> GetSequences([FromQuery] FollowUpSequenceType? sequenceType)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _followUpAutomation.GetSequencesAsync(organizationId, sequenceType);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpPut("sequences")]
    public async Task<IActionResult> UpsertSequence([FromBody] FollowUpSequenceUpsertRequestDto request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _followUpAutomation.UpsertSequenceAsync(organizationId, request);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpGet("estimates/{estimateId:guid}/runs")]
    public async Task<IActionResult> GetEstimateRuns(Guid estimateId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _followUpAutomation.GetEstimateRunsAsync(organizationId, estimateId);
        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result);
    }

    [HttpPost("estimates/{estimateId:guid}/stop")]
    public async Task<IActionResult> StopEstimateRun(Guid estimateId)
    {
        var result = await _followUpAutomation.StopEstimateSequenceAsync(estimateId, FollowUpStopReason.ManuallyStopped);
        return result.IsSuccess ? NoContent() : ProblemFrom(result);
    }

    private ObjectResult ProblemFrom(JobFlow.Business.Result result)
    {
        var problem = result.ToProblemDetails() as Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult;

        if (problem == null)
            return Problem(statusCode: 500);

        return new ObjectResult(problem.ProblemDetails)
        {
            StatusCode = problem.StatusCode
        };
    }
}
