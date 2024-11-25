using Deref.Data;

namespace Deref;

public interface IBranchDatabaseProvider
{
    Task<BranchDatabase> GetDatabaseAsync(string branchName, CancellationToken cancellationToken);
}