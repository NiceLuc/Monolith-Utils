using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MonoUtils.UseCases.InitializeDatabase;

namespace MonoUtils.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services, Assembly? assembly = null)
    {
        services.AddSingleton<ScannedFiles>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            if (assembly != null)
                config.RegisterServicesFromAssembly(assembly);
        });

        return services;
    }
}