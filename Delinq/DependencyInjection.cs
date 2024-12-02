using Delinq.CodeGeneration.Engine;
using Delinq.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonoUtils.Domain;
using MonoUtils.Infrastructure;
using Mustache;

namespace Delinq;

internal static class DependencyInjection
{
    public static void AddDelinqServices(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddTransient<ScopeTracker>();

        services.AddSharedServices(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IContextConfigProvider, ContextConfigProvider>();
        services.AddSingleton<IConfigSettingsBuilder, ConfigSettingsBuilder>();

        services.AddSingleton<IDefinitionSerializer<ContextConfig>, DefinitionSerializer<ContextConfig>>();
        services.AddSingleton<IDefinitionSerializer<RepositoryDefinition>, RepositoryDefinitionSerializer>();
        services.AddSingleton<IDefinitionSerializer<ContextDefinition>, DefinitionSerializer<ContextDefinition>>();

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

        // handlebars template support
        services.AddSingleton<FormatCompiler>();
        services.AddSingleton<ITemplateProvider, TemplateProvider>();
        services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
    }
}