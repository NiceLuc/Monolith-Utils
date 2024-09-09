namespace GVLinQOptimizer.Renderers;

internal class BaseRendererProvider
{
    private readonly IEnumerable<IRenderer<ContextDefinition>> _renderers;

    protected BaseRendererProvider(IEnumerable<IRenderer<ContextDefinition>> renderers)
    {
        _renderers = renderers;
    }

    protected IRenderer<ContextDefinition> GetRenderer(string templateName)
    {
        var renderer = _renderers.FirstOrDefault(p => p.ResourceFileName == (templateName));
        if (renderer is null)
            throw new InvalidOperationException($"No renderer class found for template '{templateName}'");

        return renderer;
    }
}