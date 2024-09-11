using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositoryInterface", "IRepository.hbs", "I{0}Repository.cs")]
internal class RepositoryInterfaceRenderer : BaseRenderer<ContextDefinition>;