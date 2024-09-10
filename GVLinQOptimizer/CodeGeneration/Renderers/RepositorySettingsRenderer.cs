using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositorySettings", "RepositorySettings.hbs", "{0}RepositorySettings.cs")]
internal class RepositorySettingsRenderer : BaseRenderer<ContextDefinition>;