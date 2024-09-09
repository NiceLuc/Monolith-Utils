using GVLinQOptimizer.Parsers;
using GVLinQOptimizer.Renders;
using Microsoft.Extensions.DependencyInjection;
using Mustache;

namespace GVLinQOptimizer.DependencyInjection
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
            services.AddSingleton<IParser<TypeDefinition>, DTOPropertyParser>();
            services.AddSingleton<ScopeTracker>();
        }

        public static void AddHandlebarsTemplateSupport(this IServiceCollection services)
        {
            services.AddSingleton<FormatCompiler>();
            services.AddSingleton<IContextDefinitionSerializer, ContextDefinitionSerializer>();
            services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
        }
    }
}
