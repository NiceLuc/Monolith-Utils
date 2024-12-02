using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;
using Serilog;

namespace MonoUtils.Infrastructure;

public static class DependencyInjection
{
    public static IHostBuilder ConfigureInfrastructureLogging(this IHostBuilder host) =>
        host.UseSerilog((context, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}");

        });

    public static IServiceCollection AddSharedServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<IFileStorage, FileStorage>();

        services.AddSingleton<IEmbeddedResourceProvider>(_
            => new EmbeddedResourceProvider(assembly));
        return services;
    }
}