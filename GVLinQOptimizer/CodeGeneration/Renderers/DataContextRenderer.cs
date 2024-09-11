using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("DataContext", "DataContext.hbs", "{0}DataContext.cs")]
internal class DataContextRenderer : BaseRenderer<ContextDefinition>;