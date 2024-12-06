using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MonoUtils.UseCases;

public static class DependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services, Assembly? assembly = null)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            if (assembly != null)
                config.RegisterServicesFromAssembly(assembly);
        });

        return services;
    }
}