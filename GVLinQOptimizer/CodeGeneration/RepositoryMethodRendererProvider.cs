using Delinq.CodeGeneration.Renderers;

namespace Delinq.CodeGeneration;

internal class RepositoryMethodRendererProvider(IEnumerable<IRenderer<MethodDefinition>> renders)
    : BaseRendererProvider<MethodDefinition>(renders);