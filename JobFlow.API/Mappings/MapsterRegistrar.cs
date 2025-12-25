using System.Reflection;
using Mapster;
using MapsterMapper;

namespace JobFlow.API.Mappings;

public static class MapsterRegistrar
{
    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}