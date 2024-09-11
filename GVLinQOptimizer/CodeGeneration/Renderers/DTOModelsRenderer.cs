using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("DTOModels", "DTOModels.hbs", "{0}DataModels.cs")]
internal class DTOModelsRenderer : BaseRenderer<ContextDefinition>;