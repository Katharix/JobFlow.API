using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    ///     Get a single job by Id (with org context).
    /// </summary>
    [HttpGet("{id:guid}/organization/{organizationId:guid}")]
    public async Task<IActionResult> GetJobById(Guid id, Guid organizationId)
    {
        var result = await _jobService.GetJobByIdAsync(id, organizationId);
        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Get jobs scheduled for a specific date.
    /// </summary>
    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetJobsByDate(DateTime date)
    {
        var result = await _jobService.GetJobsByDate(date);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Get jobs by status and organization.
    /// </summary>
    [HttpGet("status/{statusId:guid}/organization/{organizationId:guid}")]
    public async Task<IActionResult> GetJobsByStatus(Guid statusId, Guid organizationId)
    {
        var result = await _jobService.GetJobsByStatusAsync(statusId, organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Create or update a job.
    /// </summary>
    [HttpPost("{organizationId:guid}")]
    public async Task<IActionResult> UpsertJob(
        [FromRoute] Guid organizationId,
        [FromBody] Job model)
    {
        if (organizationId == Guid.Empty)
            return BadRequest("OrganizationId is required.");

        var result = await _jobService.UpsertJobAsync(model, organizationId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Delete a job.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var result = await _jobService.DeleteJobAsync(id);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}