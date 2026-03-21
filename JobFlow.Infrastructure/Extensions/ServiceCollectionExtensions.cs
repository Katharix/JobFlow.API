using JobFlow.Infrastructure.ExternalServices.ConfigurationInterfaces;
using JobFlow.Infrastructure.HttpClients;
using Microsoft.Extensions.DependencyInjection;

namespace JobFlow.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobFlowHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(JobFlowNamedClient.Brevo, (sp, client) =>
        {
            var brevoSettings = sp.GetRequiredService<IBrevoSettings>();
            client.BaseAddress = new Uri("https://api.brevo.com/v3/");
            client.DefaultRequestHeaders.Add("api-key", brevoSettings.ApiKey);
        });

        services.AddHttpClient(JobFlowNamedClient.OpenMeteo, client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        return services;
    }
}