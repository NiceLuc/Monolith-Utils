using GVLinQOptimizer.CodeGeneration.Renderers;

namespace GVLinQOptimizer.CodeGeneration;

public interface IRendererProvider<T>
{
    IRenderer<T> GetRenderer(string rendererKey);
}