using Delinq.CodeGeneration.Renderers;

namespace Delinq.CodeGeneration;

internal class RepositoryRendererProvider(IEnumerable<IRenderer<ContextDefinition>> renders) 
    : BaseRendererProvider<ContextDefinition>(renders);