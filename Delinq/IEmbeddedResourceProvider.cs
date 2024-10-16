namespace Delinq;

public interface IEmbeddedResourceProvider
{
    Task<string> GetResourceAsStringAsync(string resourceFileName, CancellationToken cancellationToken);
}