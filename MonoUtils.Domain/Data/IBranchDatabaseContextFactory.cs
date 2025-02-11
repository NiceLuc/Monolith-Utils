namespace MonoUtils.Domain.Data;

public interface IBranchDatabaseContextFactory
{
    Task<BranchDatabaseContext> CreateAsync(CancellationToken cancellationToken);
    Task<BranchDatabaseContext> CreateAsync(string branchName, CancellationToken cancellationToken);
}