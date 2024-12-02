using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MonoUtils.Domain;

namespace MonoUtils.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<IFileStorage, FileStorage>();

        services.AddSingleton<IEmbeddedResourceProvider>(_
            => new EmbeddedResourceProvider(assembly));
        return services;
    }
}