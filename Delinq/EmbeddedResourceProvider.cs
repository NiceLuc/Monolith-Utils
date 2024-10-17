namespace Delinq;

internal class EmbeddedResourceProvider(IDictionary<string, string>? resourceMap = null) : IEmbeddedResourceProvider
{
    private readonly IDictionary<string, string> _resourceMap = resourceMap ?? new Dictionary<string, string>();

    public async Task<string> GetResourceAsStringAsync(string resourceFileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(resourceFileName))
            throw new ArgumentException("Value cannot be null or empty.", nameof(resourceFileName));

        if (_resourceMap.TryGetValue(resourceFileName, out var template))
            return template;

        var assembly = typeof(Program).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        var filtered = resourceNames.Where(n => n.EndsWith($".{resourceFileName}")).ToArray();
        if (filtered.Length == 0)
            throw new InvalidOperationException($"Unable to locate resource: {resourceFileName}");

        if (filtered.Length > 1)
            throw new InvalidOperationException($"Found {filtered.Length} resources for {resourceFileName}. Expecting 1.");

        var resourceStream = assembly.GetManifestResourceStream(filtered[0]);
        if (resourceStream == null)
            throw new InvalidOperationException($"Unable to locate resource: {resourceFileName}");

        using var sr = new StreamReader(resourceStream);
        template = await sr.ReadToEndAsync(cancellationToken);
        _resourceMap.Add(resourceFileName, template);

        return template;
    }
}