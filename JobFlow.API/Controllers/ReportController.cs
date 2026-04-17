using JobFlow.API.Extensions;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [Authorize]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate)
    {
        var organizationId = HttpContext.GetOrganizationId();
        if (organizationId == Guid.Empty)
            return Unauthorized("Organization context missing.");

        var start = startDate ?? DateTimeOffset.UtcNow.AddMonths(-6);
        var end = endDate ?? DateTimeOffset.UtcNow;

        var result = await _reportService.GetDashboardAsync(organizationId, start, end);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
