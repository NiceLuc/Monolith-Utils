using GVLinQOptimizer.CodeGeneration.Renderers;

namespace GVLinQOptimizer.CodeGeneration;

internal class RepositoryRendererProvider(IEnumerable<IRenderer<ContextDefinition>> renders) 
    : BaseRendererProvider<ContextDefinition>(renders);