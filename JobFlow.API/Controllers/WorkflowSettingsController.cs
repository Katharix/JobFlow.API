using JobFlow.API.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/workflow-settings")]
public class WorkflowSettingsController : ControllerBase
{
    private readonly IWorkflowSettingsService _workflowSettings;

    public WorkflowSettingsController(IWorkflowSettingsService workflowSettings)
    {
        _workflowSettings = workflowSettings;
    }

    [HttpGet("job-statuses")]
    public async Task<IActionResult> GetJobStatuses()
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _workflowSettings.GetJobLifecycleStatusesAsync(organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("job-statuses")]
    public async Task<IActionResult> UpdateJobStatuses([FromBody] List<WorkflowStatusUpsertRequestDto> statuses)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _workflowSettings.UpsertJobLifecycleStatusesAsync(organizationId, statuses);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
