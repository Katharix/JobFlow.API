namespace JobFlow.Business.Models.DTOs;

public class WeatherForecastDto
{
    public required string Timezone { get; set; }
    public required WeatherCurrentDto Current { get; set; }
    public required List<WeatherDailyDto> Daily { get; set; }
    public required List<WeatherRiskAlertDto> RiskAlerts { get; set; }
}

public class WeatherCurrentDto
{
    public required DateTime TimestampUtc { get; set; }
    public required decimal TemperatureF { get; set; }
    public required decimal WindSpeedMph { get; set; }
    public required int WeatherCode { get; set; }
    public required string Summary { get; set; }
}

public class WeatherDailyDto
{
    public required DateOnly Date { get; set; }
    public required decimal MinTemperatureF { get; set; }
    public required decimal MaxTemperatureF { get; set; }
    public required decimal MaxWindSpeedMph { get; set; }
    public required int MaxPrecipitationProbability { get; set; }
    public required int WeatherCode { get; set; }
    public required string Summary { get; set; }
}

public class WeatherRiskAlertDto
{
    public required DateOnly Date { get; set; }
    public required string Severity { get; set; }
    public required string Message { get; set; }
}
