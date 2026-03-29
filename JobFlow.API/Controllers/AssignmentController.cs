using JobFlow.API.Extensions;
using JobFlow.API.Models;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IAssignmentGenerator _assignmentGenerator;

    public AssignmentController(
        IAssignmentService assignmentService,
        IAssignmentGenerator assignmentGenerator
        )
    {
        _assignmentService = assignmentService;
        _assignmentGenerator = assignmentGenerator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.GetAssignmentByIdAsync(organizationId, id);
        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    // Calendar range fetch
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var organizationId = HttpContext.GetOrganizationId();

        // ensure UTC
        var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end, DateTimeKind.Utc);

        // 1) auto-generate missing assignments
        var gen = await _assignmentGenerator.EnsureAssignmentsExistAsync(organizationId, startUtc, endUtc);
        if (gen.IsFailure)
            return BadRequest(gen.Error);

        // 2) fetch persisted assignments
        var result = await _assignmentService.GetAssignmentsAsync(organizationId, startUtc, endUtc);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
    // Create assignment under a job (schedule a job occurrence)
    [HttpPost("~/api/job/{jobId:guid}/assignments")]
    public async Task<IActionResult> Create(Guid jobId, [FromBody] CreateAssignmentRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.CreateAssignmentAsync(organizationId, jobId, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // Reschedule
    [HttpPut("{id:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateAssignmentScheduleRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.UpdateAssignmentScheduleAsync(organizationId, id, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // Status transitions (start/complete/skip/cancel)
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAssignmentStatusRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.UpdateAssignmentStatusAsync(organizationId, id, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // Update assignees for an assignment
    [HttpPut("{id:guid}/assignees")]
    public async Task<IActionResult> UpdateAssignees(Guid id, [FromBody] UpdateAssignmentAssigneesRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.UpdateAssignmentAssigneesAsync(organizationId, id, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    // Update assignment notes
    [HttpPut("{id:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(Guid id, [FromBody] UpdateAssignmentNotesRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _assignmentService.UpdateAssignmentNotesAsync(organizationId, id, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }


}
