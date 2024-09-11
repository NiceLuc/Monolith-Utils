using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("TestUtils", "TestUtils.hbs", "TestUtils.cs")]
internal class TestUtilsRenderer : BaseRenderer<ContextDefinition>;