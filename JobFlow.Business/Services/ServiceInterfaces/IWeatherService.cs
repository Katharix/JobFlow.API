using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IWeatherService
{
    Task<WeatherForecastDto> GetForecastAsync(double latitude, double longitude, int days = 5, CancellationToken cancellationToken = default);
}
