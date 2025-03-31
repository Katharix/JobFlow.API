using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace JobFlow.Infrastructure.DI
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
                if (!interfaces.Any()) continue;

                foreach (var iface in interfaces)
                {
                    if (type.GetCustomAttribute<ScopedServiceAttribute>() != null)
                    {
                        services.AddScoped(iface, type);
                        Console.WriteLine($"[DI] Registered Scoped: {iface.Name} → {type.Name}");
                    }
                    else if (type.GetCustomAttribute<SingletonServiceAttribute>() != null)
                    {
                        services.AddSingleton(iface, type);
                        Console.WriteLine($"[DI] Registered Singleton: {iface.Name} → {type.Name}");
                    }
                    else if (type.GetCustomAttribute<TransientServiceAttribute>() != null)
                    {
                        services.AddTransient(iface, type);
                        Console.WriteLine($"[DI] Registered Transient: {iface.Name} → {type.Name}");
                    }
                }
            }

            return services;
        }
    }
}
