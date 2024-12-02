using System.Text;
using System.Text.Json;
using MonoUtils.Domain;

namespace Delinq;

internal class ContextConfigProvider(IEmbeddedResourceProvider resourceProvider, IDictionary<string, ContextConfig>? cache = null) : IContextConfigProvider
{
    private readonly IDictionary<string, ContextConfig> _cache = cache ?? new Dictionary<string, ContextConfig>();

    public async Task<ContextConfig> GetContextConfigAsync(string contextName, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(contextName, out var config))
            return config;

        var resourceFileName = $"{contextName}.json";
        var json = await resourceProvider.GetResourceAsStringAsync(resourceFileName, cancellationToken);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        config = await JsonSerializer.DeserializeAsync<ContextConfig>(stream, cancellationToken: cancellationToken);
        if (config == null)
            throw new InvalidOperationException("Unable to deserialize json file: " + resourceFileName);

        _cache.Add(contextName, config);

        return config;
    }
}