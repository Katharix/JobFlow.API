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
                        Console.WriteLine($"[DI] Registered Scoped: {iface.Name} → {type.Name}");
                    }
                    else if (isSingleton)
                    {
                        services.AddSingleton(iface, type);
                        Console.WriteLine($"[DI] Registered Singleton: {iface.Name} → {type.Name}");
                    }
                    else if (isTransient)
                    {
                        services.AddTransient(iface, type);
                        Console.WriteLine($"[DI] Registered Transient: {iface.Name} → {type.Name}");
                    }
                }

                // Register the concrete class itself
                if (isScoped)
                {
                    services.AddScoped(type);
                    Console.WriteLine($"[DI] Registered Scoped (concrete): {type.Name}");
                }
                else if (isSingleton)
                {
                    services.AddSingleton(type);
                    Console.WriteLine($"[DI] Registered Singleton (concrete): {type.Name}");
                }
                else if (isTransient)
                {
                    services.AddTransient(type);
                    Console.WriteLine($"[DI] Registered Transient (concrete): {type.Name}");
                }
            }

            return services;
        }
    }
}
