using CommandLine;
using Delinq.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;
using MonoUtils.Infrastructure;

namespace Delinq;

internal static class DependencyInjection
{
    public static IServiceCollection AddDelinqServices(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton<IContextConfigProvider, ContextConfigProvider>();
        services.AddSingleton<IConfigSettingsBuilder, ConfigSettingsBuilder>();

        services.AddSingleton<IDefinitionSerializer<ContextConfig>, DefinitionSerializer<ContextConfig>>();
        services.AddSingleton<IDefinitionSerializer<ContextDefinition>, DefinitionSerializer<ContextDefinition>>();
        services.AddSingleton<IDefinitionSerializer<RepositoryDefinition>, RepositoryDefinitionSerializer>();

        services.Configure<ConnectionStrings>(context.Configuration.GetSection("ConnectionStrings"));
        services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

        services.AddTransient<ConnectionStrings>();
        services.AddSingleton<AppSettings>();

        // used for Initialize.Handler() constructor
        services.AddSingleton<IParser<ContextDefinition>, NamespaceParser>();
        services.AddSingleton<IParser<ContextDefinition>, ContextClassParser>();
        services.AddSingleton<IParser<ContextDefinition>, MethodParser>();
        services.AddSingleton<IParser<ContextDefinition>, DTOClassParser>();
        services.AddSingleton<IParser<MethodDefinition>, ParametersParser>();
        services.AddSingleton<IParser<DTOClassDefinition>, DTOPropertyParser>();

        services.AddSingleton(_ => new Parser(config =>
        {
            config.CaseInsensitiveEnumValues = true;
            config.HelpWriter = Console.Out;
            config.AutoHelp = true;
            config.AutoVersion = true;
        }));

        return services;
    }
}