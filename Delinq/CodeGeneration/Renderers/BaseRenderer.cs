using System.Reflection;
using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

internal abstract class BaseRenderer<T> : IRenderer<T>
{
    public string Key { get; }
    public string ResourceFileName { get; }
    public string? FileNameFormat { get; }

    protected BaseRenderer()
    {
        var attribute = GetType().GetCustomAttribute<HandlebarsTemplateModelAttribute>();
        if (attribute == null) throw new ArgumentNullException(nameof(attribute));
        if (string.IsNullOrEmpty(attribute.ResourceFileName))
            throw new ArgumentNullException(nameof(attribute.ResourceFileName));

        Key = attribute.Key;
        ResourceFileName = attribute.ResourceFileName;
        FileNameFormat = attribute.FileNameFormat;
    }

    public async Task<string> RenderAsync(ITemplateEngine engine, T data, CancellationToken cancellationToken)
    {
        var viewModel = await ConvertToViewModelAsync(engine, data, cancellationToken);
        return await engine.ProcessAsync(ResourceFileName, viewModel, cancellationToken);
    }

    protected virtual Task<object> ConvertToViewModelAsync(ITemplateEngine engine, T definition,
        CancellationToken cancellationToken) => Task.FromResult<object>(definition);
}