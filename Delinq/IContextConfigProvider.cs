namespace Delinq;

public interface IContextConfigProvider
{
    Task<ContextConfig> GetContextConfigAsync(string contextName, CancellationToken cancellationToken);
}