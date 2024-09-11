using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("UnitTests", "UnitTests.hbs", "{0}RepositoryTests.cs")]
internal class UnitTestsRenderer : BaseRenderer<ContextDefinition>;