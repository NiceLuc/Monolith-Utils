using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositorySettingsInterface", "IRepositorySettings.hbs", "I{0}RepositorySettings.cs")]
internal class RepositorySettingsInterfaceRenderer : BaseRenderer<ContextDefinition>;