using JobFlow.API.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispatchController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IAssignmentGenerator _assignmentGenerator;
    private readonly IEmployeeService _employeeService;
    private readonly IJobService _jobService;

    public DispatchController(
        IAssignmentService assignmentService,
        IAssignmentGenerator assignmentGenerator,
        IEmployeeService employeeService,
        IJobService jobService)
    {
        _assignmentService = assignmentService;
        _assignmentGenerator = assignmentGenerator;
        _employeeService = employeeService;
        _jobService = jobService;
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end, DateTimeKind.Utc);

        var gen = await _assignmentGenerator.EnsureAssignmentsExistAsync(organizationId, startUtc, endUtc);
        if (gen.IsFailure)
            return BadRequest(gen.Error);

        var assignmentsResult = await _assignmentService.GetAssignmentsAsync(organizationId, startUtc, endUtc);
        if (assignmentsResult.IsFailure)
            return BadRequest(assignmentsResult.Error);

        var employeesResult = await _employeeService.GetByOrganizationIdAsync(organizationId);
        if (employeesResult.IsFailure)
            return BadRequest(employeesResult.Error);

        var jobsResult = await _jobService.GetJobsAsync(organizationId);
        if (jobsResult.IsFailure)
            return BadRequest(jobsResult.Error);

        var unscheduledJobs = jobsResult.Value
            .Where(job => job.HasAssignments == false)
            .Select(job => new DispatchUnscheduledJobDto
            {
                JobId = job.Id ?? Guid.Empty,
                JobTitle = job.Title,
                ClientName = job.OrganizationClient != null
                    ? $"{job.OrganizationClient.FirstName} {job.OrganizationClient.LastName}".Trim()
                    : null,
                JobLifecycleStatus = job.LifecycleStatus,
                Notes = job.Comments
            })
            .ToList();

        var response = new DispatchBoardDto
        {
            RangeStart = startUtc,
            RangeEnd = endUtc,
            Assignments = assignmentsResult.Value,
            Employees = employeesResult.Value,
            UnscheduledJobs = unscheduledJobs
        };

        return Ok(response);
    }
}
