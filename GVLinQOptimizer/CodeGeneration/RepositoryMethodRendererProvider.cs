using GVLinQOptimizer.CodeGeneration.Renderers;

namespace GVLinQOptimizer.CodeGeneration;

internal class RepositoryMethodRendererProvider(IEnumerable<IRenderer<MethodDefinition>> renders)
    : BaseRendererProvider<MethodDefinition>(renders);