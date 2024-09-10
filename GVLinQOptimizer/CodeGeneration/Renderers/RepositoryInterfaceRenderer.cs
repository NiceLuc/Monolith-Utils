using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositoryInterface", "IRepository.hbs", "I{0}Repository.cs")]
internal class RepositoryInterfaceRenderer : BaseRenderer<ContextDefinition>;