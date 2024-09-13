using Delinq.CodeGeneration;
using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.Renderers;
using Delinq.Parsers;
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
            services.AddSingleton<ScopeTracker>();
        }

        public static void AddHandlebarsTemplateSupport(this IServiceCollection services)
        {
            services.AddSingleton<FormatCompiler>();
            services.AddSingleton<ITemplateProvider, TemplateProvider>();
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddSingleton<IContextDefinitionSerializer, ContextDefinitionSerializer>();

            // used for resolving renderer instances
            services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositorySettingsInterfaceRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositorySettingsRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositoryInterfaceRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, RepositoryRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, DataContextRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, DTOModelsRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, TestUtilsRenderer>();
            services.AddSingleton<IRenderer<ContextDefinition>, UnitTestsRenderer>();
            services.AddSingleton<IRendererProvider<ContextDefinition>, RendererProvider<ContextDefinition>>();
        }
    }
}
