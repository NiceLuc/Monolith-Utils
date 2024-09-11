using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("UnitTestMethod")]
internal class UnitTestMethodRenderer: BaseRenderer<MethodDefinition>;