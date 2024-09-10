using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("DTOModels", "DTOModels.hbs", "{0}DataModels.cs")]
internal class DTOModelsRenderer : BaseRenderer<ContextDefinition>;