using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;

namespace MonoUtils.App;

internal static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<IProgramSettingsBuilder, ProgramSettingsBuilder>();

        services.AddSingleton(_ => new Parser(config =>
        {
            config.CaseInsensitiveEnumValues = true;
            config.HelpWriter = Console.Out;
            config.AutoHelp = true;
            config.AutoVersion = true;
        }));

        services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

        return services;
    }
}