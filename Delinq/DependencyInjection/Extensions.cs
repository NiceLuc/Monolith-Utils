using Delinq.CodeGeneration.Engine;
using Delinq.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Mustache;

namespace Delinq.DependencyInjection
{
    internal static class Extensions
    {
        public static void AddCustomDesignerParsers(this IServiceCollection services)
        {
            services.AddTransient<ScopeTracker>();
            services.AddSingleton<IFileStorage, FileStorage>();

            // used for Initialize.Handler() constructor
            services.AddSingleton<IParser<ContextDefinition>, NamespaceParser>();
            services.AddSingleton<IParser<ContextDefinition>, ContextClassParser>();
            services.AddSingleton<IParser<ContextDefinition>, MethodParser>();
            services.AddSingleton<IParser<ContextDefinition>, DTOClassParser>();
            services.AddSingleton<IParser<MethodDefinition>, ParametersParser>();
            services.AddSingleton<IParser<DTOClassDefinition>, DTOPropertyParser>();
            services.AddSingleton<IDefinitionSerializer<ContextDefinition>, DefinitionSerializer<ContextDefinition>>();

            // used for VerifySprocs.Handler() constructor
            services.AddSingleton<IDefinitionSerializer<RepositoryDefinition>, DefinitionSerializer<RepositoryDefinition>>();
        }

        public static void AddHandlebarsTemplateSupport(this IServiceCollection services)
        {
            services.AddSingleton<FormatCompiler>();
            services.AddSingleton<ITemplateProvider, TemplateProvider>();
            services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
        }
    }
}
