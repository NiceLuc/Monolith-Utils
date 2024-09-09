using GVLinQOptimizer.Parsers;
using GVLinQOptimizer.Renderers;
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
            services.AddSingleton<IParser<DTOClassDefinition>, DTOPropertyParser>();
            services.AddSingleton<ScopeTracker>();
        }

        public static void AddHandlebarsTemplateSupport(this IServiceCollection services)
        {
            services.AddSingleton<FormatCompiler>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositorySettingsInterfaceRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositorySettingsRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositoryInterfaceRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositoryRenderer>();
            services.AddSingleton<IRenderer<MethodDefinition>, RepositoryMethodRenderer>();
            services.AddSingleton<IRepositoryRendererProvider, RepositoryRendererProvider>();

            services.AddSingleton<IContextDefinitionSerializer, ContextDefinitionSerializer>();
            services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
        }
    }
}
