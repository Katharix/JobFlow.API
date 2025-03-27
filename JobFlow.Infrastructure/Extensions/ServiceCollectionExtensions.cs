using JobFlow.Infrastructure.HttpClients;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;


namespace JobFlow.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJobFlowHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient(JobFlowNamedClient.Brevo, client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com/v3/");
            });

            return services;
        }
    }
}
