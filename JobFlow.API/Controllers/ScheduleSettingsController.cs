using JobFlow.API.Extensions;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/schedule-settings")]
public class ScheduleSettingsController : ControllerBase
{
    private readonly IScheduleSettingsService _scheduleSettings;

    public ScheduleSettingsController(IScheduleSettingsService scheduleSettings)
    {
        _scheduleSettings = scheduleSettings;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _scheduleSettings.GetScheduleSettingsAsync(organizationId);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ScheduleSettingsUpsertRequestDto dto)
    {
        var organizationId = HttpContext.GetOrganizationId();

        var result = await _scheduleSettings.UpsertScheduleSettingsAsync(organizationId, dto);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
