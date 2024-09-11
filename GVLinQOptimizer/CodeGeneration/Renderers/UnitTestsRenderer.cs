using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("UnitTests", "UnitTests.hbs", "{0}RepositoryTests.cs")]
internal class UnitTestsRenderer : BaseRenderer<ContextDefinition>;