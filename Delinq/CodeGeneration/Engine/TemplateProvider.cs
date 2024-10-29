using SharedKernel;

namespace Delinq.CodeGeneration.Engine;

internal class TemplateProvider(IDictionary<string, string>? resourceMap = null) : ITemplateProvider
{
    private readonly EmbeddedResourceProvider _provider = new(typeof(Program).Assembly, resourceMap);

    public Task<string> GetTemplateAsync(string resourceFileName, CancellationToken cancellationToken) 
        => _provider.GetResourceAsStringAsync(resourceFileName, cancellationToken);
}