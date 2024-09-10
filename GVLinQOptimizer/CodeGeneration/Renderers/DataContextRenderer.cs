using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

[HandlebarsTemplateModel("DataContext", "DataContext.hbs", "{0}DataContext.cs")]
internal class DataContextRenderer : BaseRenderer<ContextDefinition>;