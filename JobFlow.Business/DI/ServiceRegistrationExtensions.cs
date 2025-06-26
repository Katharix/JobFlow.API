using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace JobFlow.Business.DI
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddAttributedServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            var serviceTypes = assemblies
                .SelectMany(x => x.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && (
                    t.GetCustomAttribute<ScopedServiceAttribute>() != null ||
                    t.GetCustomAttribute<SingletonServiceAttribute>() != null ||
                    t.GetCustomAttribute<TransientServiceAttribute>() != null
                ));

            foreach (var type in serviceTypes)
            {
                var interfaces = type.GetInterfaces();

                bool isScoped = type.GetCustomAttribute<ScopedServiceAttribute>() != null;
                bool isSingleton = type.GetCustomAttribute<SingletonServiceAttribute>() != null;
                bool isTransient = type.GetCustomAttribute<TransientServiceAttribute>() != null;

                // Register each interface
                foreach (var iface in interfaces)
                {
                    if (isScoped)
                    {
                        services.AddScoped(iface, type);
                    }
                    else if (isSingleton)
                    {
                        services.AddSingleton(iface, type);
                    }
                    else if (isTransient)
                    {
                        services.AddTransient(iface, type);
                    }
                }

                // Register the concrete class itself
                if (isScoped)
                {
                    services.AddScoped(type);
                }
                else if (isSingleton)
                {
                    services.AddSingleton(type);
                }
                else if (isTransient)
                {
                    services.AddTransient(type);
                }
            }

            return services;
        }
    }
}
