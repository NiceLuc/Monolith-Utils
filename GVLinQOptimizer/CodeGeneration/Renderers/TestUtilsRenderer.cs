using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("TestUtils", "TestUtils.hbs", "TestUtils.cs")]
internal class TestUtilsRenderer : BaseRenderer<ContextDefinition>;