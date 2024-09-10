using GVLinQOptimizer.CodeGeneration.Renderers;

namespace GVLinQOptimizer.CodeGeneration;

public interface IRepositoryMethodRendererProvider
{
    IRenderer<MethodDefinition> GetNonQueryRendererAsync();
    IRenderer<MethodDefinition> GetQueryManyRendererAsync();
    IRenderer<MethodDefinition> GetQuerySingleRendererAsync();
}