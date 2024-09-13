using Delinq.CodeGeneration.Renderers;

namespace Delinq.CodeGeneration;

internal class RendererProvider<T>(IEnumerable<IRenderer<T>> renderers) : IRendererProvider<T>
{
    private readonly Dictionary<string, IRenderer<T>> _renderMap = renderers.ToDictionary(r => r.Key);

    public IRenderer<T> GetRenderer(string key)
    {
        if (!_renderMap.TryGetValue(key, out var renderer))
            throw new InvalidOperationException($"No renderer class found for '{key}'. Make sure it is registered with IoC.");

        return renderer;
    }
}