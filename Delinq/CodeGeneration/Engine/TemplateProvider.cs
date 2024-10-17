namespace Delinq.CodeGeneration.Engine;

internal class TemplateProvider(IDictionary<string, string>? resourceMap = null) : ITemplateProvider
{
    private readonly EmbeddedResourceProvider _provider = new(resourceMap);

    public Task<string> GetTemplateAsync(string resourceFileName, CancellationToken cancellationToken) 
        => _provider.GetResourceAsStringAsync(resourceFileName, cancellationToken);
}