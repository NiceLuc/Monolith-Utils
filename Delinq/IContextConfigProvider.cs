namespace Delinq;

public interface IContextConfigProvider
{
    Task<ContextConfig> GetContextConfigAsync(string contextName, string branchName, CancellationToken cancellationToken);
}