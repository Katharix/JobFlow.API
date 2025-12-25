using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobTrackingController : ControllerBase
{
    private readonly JobTrackingService _jobTrackingService;

    public JobTrackingController(JobTrackingService jobTrackingService)
    {
        _jobTrackingService = jobTrackingService;
    }

    /// <summary>
    ///     Records a new GPS location update for a job.
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> UpdateLocation([FromBody] JobTrackingUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _jobTrackingService.RecordLocationAsync(dto);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { success = true });
    }
}