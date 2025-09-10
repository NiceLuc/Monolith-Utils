namespace MonoUtils.Delinq;

public interface IConfigSettingsBuilder
{
    Task<ConfigSettings> BuildAsync(string contextName, string branchName, CancellationToken cancellationToken);
}