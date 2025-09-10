using MonoUtils.Domain.Data;

namespace MonoUtils.Domain;

public interface IBranchDatabaseProvider
{
    Task<BranchDatabase> GetDatabaseAsync(string branchName, CancellationToken cancellationToken);
}