using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] int days = 5,
        CancellationToken cancellationToken = default)
    {
        if (lat is < -90 or > 90)
            return BadRequest("Latitude must be between -90 and 90.");

        if (lon is < -180 or > 180)
            return BadRequest("Longitude must be between -180 and 180.");

        var forecast = await _weatherService.GetForecastAsync(lat, lon, days, cancellationToken);
        return Ok(forecast);
    }
}
