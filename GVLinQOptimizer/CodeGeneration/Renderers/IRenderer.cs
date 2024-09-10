using GVLinQOptimizer.CodeGeneration.Engine;

namespace GVLinQOptimizer.CodeGeneration.Renderers;

public interface IRenderer<T>
{
    string Key { get; }
    string ResourceFileName { get; }
    string FileNameFormat { get; }

    Task<string> RenderAsync(ITemplateEngine engine, T data, CancellationToken cancellationToken);
}