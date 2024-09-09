using System.Reflection;

namespace GVLinQOptimizer.Renderers;

internal abstract class BaseRenderer<T> : IRenderer<T>
{
    public string ResourceFileName { get; }
    public string FileNameFormat { get; }

    protected BaseRenderer()
    {
        var attribute = GetType().GetCustomAttribute<HandlebarsTemplateModelAttribute>();
        if (attribute == null) throw new ArgumentNullException(nameof(attribute));

        ResourceFileName = attribute.ResourceFileName;
        FileNameFormat = attribute.FileNameFormat;
    }

    public virtual Task<string> RenderAsync(ITemplateEngine engine, object data, CancellationToken cancellationToken)
    {
        return engine.ProcessAsync(ResourceFileName, data!, cancellationToken);
    }

    public virtual Task<object> ToViewModelAsync(ITemplateEngine engine, T definition, CancellationToken cancellationToken)
    {
        return Task.FromResult<object>(definition);
    }
}