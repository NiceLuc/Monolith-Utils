using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel;

namespace Deref;

internal static class DependencyInjection
{
    public static IServiceCollection AddDerefServices(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSharedServices(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IProgramSettingsBuilder, ProgramSettingsBuilder>();
        services.AddSingleton<IDefinitionSerializer<BranchSchema>, DefinitionSerializer<BranchSchema>>();
        services.AddSingleton<IDefinitionSerializer<ProgramConfig>, DefinitionSerializer<ProgramConfig>>();

        services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

        return services;
    }
}