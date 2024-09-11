using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("RepositorySettings", "RepositorySettings.hbs", "{0}RepositorySettings.cs")]
internal class RepositorySettingsRenderer : BaseRenderer<ContextDefinition>;