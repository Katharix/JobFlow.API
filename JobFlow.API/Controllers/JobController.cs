using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IJobRecurrenceService _recurrenceService;
    private readonly IMapper _mapper;

    public JobController(IJobService jobService, IJobRecurrenceService recurrenceService, IMapper mapper)
    {
        _jobService = jobService;
        _recurrenceService = recurrenceService;
        _mapper = mapper;
    }

    /// <summary>
    ///     Get a single job by Id (with org context).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _jobService.GetJobByIdAsync(id, organizationId);
        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Get jobs by status and organization.
    /// </summary>
    [HttpGet("status/{status:int}")]
    public async Task<IActionResult> GetJobsByStatus(JobLifecycleStatus status)
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _jobService.GetJobsByStatusAsync(organizationId, status);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Create or update a job.
    /// </summary>

    [HttpPost("upsert")]
    public async Task<IActionResult> UpsertJob([FromBody] JobDto model)
    {
        var orgId = HttpContext.GetOrganizationId();

        if (orgId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var mappedJob = _mapper.Map<JobDto, Job>(model);

        var result = await _jobService.UpsertJobAsync(mappedJob, orgId);

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

    [HttpGet("all")]
    public async Task<IActionResult> GetJobs()
    {
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _jobService.GetJobsAsync(organizationId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{jobId:guid}/recurrence")]
    public async Task<IActionResult> UpsertRecurrence(Guid jobId, [FromBody] JobRecurrenceUpsertRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await _recurrenceService.UpsertAsync(jobId, organizationId, request);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }


}