using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IJobRecurrenceService _recurrenceService;
    private readonly IJobUpdateService _jobUpdates;
    private readonly IMapper _mapper;

    public JobController(
        IJobService jobService,
        IJobRecurrenceService recurrenceService,
        IJobUpdateService jobUpdates,
        IMapper mapper)
    {
        _jobService = jobService;
        _recurrenceService = recurrenceService;
        _jobUpdates = jobUpdates;
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
        var organizationId = HttpContext.GetOrganizationId();
        var result = await _jobService.DeleteJobAsync(id, organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? cursor = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? statusKey = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var hasFilters = !string.IsNullOrWhiteSpace(statusKey)
            || clientId.HasValue
            || assigneeId.HasValue
            || !string.IsNullOrWhiteSpace(search)
            || !string.IsNullOrWhiteSpace(sortBy)
            || !string.IsNullOrWhiteSpace(sortDirection);

        if (!pageSize.HasValue && string.IsNullOrWhiteSpace(cursor) && !hasFilters)
        {
            var legacyResult = await _jobService.GetJobsAsync(organizationId);
            if (legacyResult.IsFailure)
                return BadRequest(legacyResult.Error);

            return Ok(legacyResult.Value);
        }

        var result = await _jobService.GetJobsPagedAsync(
            organizationId,
            Math.Clamp(pageSize ?? 50, 1, 100),
            cursor,
            statusKey,
            clientId,
            assigneeId,
            search,
            sortBy,
            sortDirection);

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

    [HttpPut("{jobId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid jobId, [FromBody] UpdateJobStatusRequestDto request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await _jobService.UpdateJobStatusAsync(organizationId, jobId, request.Status);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{jobId:guid}/updates")]
    public async Task<IActionResult> GetJobUpdates(Guid jobId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await _jobUpdates.GetByJobAsync(jobId, organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{jobId:guid}/updates")]
    [RequestSizeLimit(55_000_000)]
    public async Task<IActionResult> CreateJobUpdate(
        Guid jobId,
        [FromForm] CreateJobUpdateFormRequest request)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var uploads = new List<JobUpdateAttachmentUpload>();
        if (request.Attachments is not null)
        {
            foreach (var file in request.Attachments)
            {
                if (file.Length <= 0)
                    continue;

                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                uploads.Add(new JobUpdateAttachmentUpload(
                    file.FileName,
                    file.ContentType,
                    stream.ToArray(),
                    file.Length));
            }
        }

        var createRequest = new CreateJobUpdateRequest(
            request.Type,
            request.Message,
            request.Status,
            uploads);

        var result = await _jobUpdates.CreateAsync(jobId, organizationId, createRequest);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{jobId:guid}/updates/{updateId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadJobUpdateAttachment(
        Guid jobId,
        Guid updateId,
        Guid attachmentId)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var result = await _jobUpdates.GetAttachmentAsync(jobId, updateId, attachmentId, organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }


}

public record CreateJobUpdateFormRequest(
    JobUpdateType Type,
    string? Message,
    JobLifecycleStatus? Status,
    List<IFormFile>? Attachments);