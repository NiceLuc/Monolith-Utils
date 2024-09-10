using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositorySettingsInterface", "IRepositorySettings.hbs", "I{0}RepositorySettings.cs")]
internal class RepositorySettingsInterfaceRenderer : BaseRenderer<ContextDefinition>;