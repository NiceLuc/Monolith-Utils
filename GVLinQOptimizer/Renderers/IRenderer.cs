namespace GVLinQOptimizer.Renderers;

public interface IRenderer<T>
{
    string ResourceFileName { get; }
    string FileNameFormat { get; }

    Task<object> ToViewModelAsync(ITemplateEngine engine, T definition, CancellationToken cancellationToken);
    Task<string> RenderAsync(ITemplateEngine engine, object data, CancellationToken cancellationToken);
}