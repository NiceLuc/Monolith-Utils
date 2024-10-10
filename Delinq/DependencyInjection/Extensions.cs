using Delinq.CodeGeneration.Engine;
using Delinq.Parsers;
using Delinq.Parsers.DesignerFile;
using Delinq.Parsers.RepositoryFile;
using Microsoft.Extensions.DependencyInjection;
using Mustache;

namespace Delinq.DependencyInjection
{
    internal static class Extensions
    {
        public static void AddCustomDesignerParsers(this IServiceCollection services)
        {
            // used for Initialize.Handler() constructor
            services.AddSingleton<IParser<ContextDefinition>, NamespaceParser>();
            services.AddSingleton<IParser<ContextDefinition>, ContextClassParser>();
            services.AddSingleton<IParser<ContextDefinition>, MethodParser>();
            services.AddSingleton<IParser<ContextDefinition>, DTOClassParser>();

            // types that are used in context definition parsers
            services.AddSingleton<IParser<MethodDefinition>, ParametersParser>();
            services.AddSingleton<IParser<DTOClassDefinition>, DTOPropertyParser>();

            // used for VerifySprocs.Handler() constructor
            services.AddSingleton<IParser<RepositoryDefinition>, EnumerableMethodParser>();

            services.AddTransient<ScopeTracker>();
        }

        public static void AddHandlebarsTemplateSupport(this IServiceCollection services)
        {
            services.AddSingleton<FormatCompiler>();
            services.AddSingleton<ITemplateProvider, TemplateProvider>();
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddSingleton<IContextDefinitionSerializer, ContextDefinitionSerializer>();

            // used for resolving renderer instances
            services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
        }
    }
}
