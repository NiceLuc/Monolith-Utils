using Delinq.CodeGeneration.Renderers;

namespace Delinq.CodeGeneration;

public interface IRendererProvider<T>
{
    IRenderer<T> GetRenderer(string rendererKey);
}