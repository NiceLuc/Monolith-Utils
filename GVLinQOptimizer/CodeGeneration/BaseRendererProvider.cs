using GVLinQOptimizer.CodeGeneration.Renderers;

namespace GVLinQOptimizer.CodeGeneration;

internal class BaseRendererProvider<T> : IRendererProvider<T>
{
    private readonly IEnumerable<IRenderer<T>> _renderers;

    protected BaseRendererProvider(IEnumerable<IRenderer<T>> renderers)
    {
        _renderers = renderers;
    }

    public IRenderer<T> GetRenderer(string key)
    {
        var renderer = _renderers.FirstOrDefault(r => r.Key == key);
        if (renderer is null)
            throw new InvalidOperationException($"No renderer class found for '{key}'. Make sure it is registered with IoC.");

        return renderer;
    }
}