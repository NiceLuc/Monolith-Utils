using System.Reflection;
using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

internal abstract class BaseRenderer<T> : IRenderer<T>
{
    public string Key { get; }
    public string? ResourceFileName { get; }
    public string? FileNameFormat { get; }

    protected BaseRenderer()
    {
        var attribute = GetType().GetCustomAttribute<HandlebarsTemplateModelAttribute>();
        if (attribute == null) throw new ArgumentNullException(nameof(attribute));

        Key = attribute.Key;
        ResourceFileName = attribute.ResourceFileName;
        FileNameFormat = attribute.FileNameFormat;
    }

    public async Task<string> RenderAsync(ITemplateEngine engine, T data, CancellationToken cancellationToken)
    {
        var resourceName = GetResourceFileName(data);
        var viewModel = await ConvertToViewModelAsync(engine, data, cancellationToken);
        return await engine.ProcessAsync(resourceName, viewModel, cancellationToken);
    }

    protected virtual string GetResourceFileName(T data)
    {
        if (string.IsNullOrEmpty(ResourceFileName))
            throw new InvalidOperationException("Must override this method to calculate the renderer key");

        return ResourceFileName;
    }

    protected virtual Task<object> ConvertToViewModelAsync(ITemplateEngine engine, T definition,
        CancellationToken cancellationToken) => Task.FromResult<object>(definition);
}