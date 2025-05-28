using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace Deref;

internal static class DependencyInjection
{
    public static IServiceCollection AddDerefServices(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<IProgramSettingsBuilder, ProgramSettingsBuilder>();
        services.AddSingleton<IBranchDatabaseProvider, BranchDatabaseProvider>();

        services.AddSingleton<IDefinitionSerializer<BranchDatabase>, BranchDatabaseSerializer>();
        services.AddSingleton<IDefinitionSerializer<ProgramConfig>, DefinitionSerializer<ProgramConfig>>();

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