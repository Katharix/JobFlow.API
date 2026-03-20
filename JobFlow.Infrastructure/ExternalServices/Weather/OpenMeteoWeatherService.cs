using System.Text.Json;
using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;

namespace JobFlow.Infrastructure.ExternalServices.Weather;

[ScopedService]
public class OpenMeteoWeatherService : IWeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenMeteoWeatherService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WeatherForecastDto> GetForecastAsync(
        double latitude, double longitude, int days = 5, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 7);

        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,wind_speed_10m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto&forecast_days={days}";
        var client = _httpClientFactory.CreateClient("OpenMeteo");

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var response = await client.GetAsync(url, linkedCts.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: linkedCts.Token);

            var root = doc.RootElement;
            var timezone = root.TryGetProperty("timezone", out var tzElement) ? tzElement.GetString() ?? "UTC" : "UTC";

            var current = ParseCurrent(root.GetProperty("current"));
            var daily = ParseDaily(root.GetProperty("daily"));

            return new WeatherForecastDto
            {
                Timezone = timezone,
                Current = current,
                Daily = daily,
                RiskAlerts = BuildRiskAlerts(daily)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // caller aborted request
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException("OpenMeteo request timed out.");
        }
    }

    private static WeatherCurrentDto ParseCurrent(JsonElement current)
    {
        var time = current.GetProperty("time").GetString() ?? DateTime.UtcNow.ToString("O");
        var timestamp = DateTime.TryParse(time, out var parsed) ? parsed.ToUniversalTime() : DateTime.UtcNow;

        var weatherCode = current.GetProperty("weather_code").GetInt32();

        return new WeatherCurrentDto
        {
            TimestampUtc = timestamp,
            TemperatureF = Convert.ToDecimal(current.GetProperty("temperature_2m").GetDouble()),
            WindSpeedMph = Convert.ToDecimal(current.GetProperty("wind_speed_10m").GetDouble()),
            WeatherCode = weatherCode,
            Summary = ToSummary(weatherCode)
        };
    }

    private static List<WeatherDailyDto> ParseDaily(JsonElement daily)
    {
        var dates = daily.GetProperty("time").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();
        var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var maxWind = daily.GetProperty("wind_speed_10m_max").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var precipProb = daily.GetProperty("precipitation_probability_max").EnumerateArray().Select(x => x.GetInt32()).ToArray();
        var weatherCodes = daily.GetProperty("weather_code").EnumerateArray().Select(x => x.GetInt32()).ToArray();

        var result = new List<WeatherDailyDto>(dates.Length);
        for (var i = 0; i < dates.Length; i++)
        {
            var date = DateOnly.TryParse(dates[i], out var parsedDate) ? parsedDate : DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(i));
            var weatherCode = weatherCodes[i];

            result.Add(new WeatherDailyDto
            {
                Date = date,
                MinTemperatureF = Convert.ToDecimal(minTemps[i]),
                MaxTemperatureF = Convert.ToDecimal(maxTemps[i]),
                MaxWindSpeedMph = Convert.ToDecimal(maxWind[i]),
                MaxPrecipitationProbability = precipProb[i],
                WeatherCode = weatherCode,
                Summary = ToSummary(weatherCode)
            });
        }

        return result;
    }

    private static List<WeatherRiskAlertDto> BuildRiskAlerts(IEnumerable<WeatherDailyDto> daily)
    {
        var alerts = new List<WeatherRiskAlertDto>();

        foreach (var day in daily)
        {
            if (day.MaxPrecipitationProbability >= 70)
            {
                alerts.Add(new WeatherRiskAlertDto
                {
                    Date = day.Date,
                    Severity = "High",
                    Message = $"High rain risk ({day.MaxPrecipitationProbability}%). Outdoor jobs may be impacted."
                });
            }
            else if (day.MaxPrecipitationProbability >= 50)
            {
                alerts.Add(new WeatherRiskAlertDto
                {
                    Date = day.Date,
                    Severity = "Medium",
                    Message = $"Moderate rain risk ({day.MaxPrecipitationProbability}%). Consider schedule buffers."
                });
            }

            if (day.MaxWindSpeedMph >= 25)
            {
                alerts.Add(new WeatherRiskAlertDto
                {
                    Date = day.Date,
                    Severity = day.MaxWindSpeedMph >= 35 ? "High" : "Medium",
                    Message = $"High wind expected ({day.MaxWindSpeedMph:0.#} mph). Exterior work risk increased."
                });
            }
        }

        return alerts;
    }

    private static string ToSummary(int code) => code switch
    {
        0 => "Clear",
        1 or 2 => "Partly Cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing Drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing Rain",
        71 or 73 or 75 => "Snow",
        77 => "Snow Grains",
        80 or 81 or 82 => "Rain Showers",
        85 or 86 => "Snow Showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with Hail",
        _ => "Unknown"
    };
}
