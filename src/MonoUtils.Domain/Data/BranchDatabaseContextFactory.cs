namespace MonoUtils.Domain.Data;

public class BranchDatabaseContextFactory(IProgramSettingsBuilder settingsBuilder, IBranchDatabaseProvider databaseProvider) : IBranchDatabaseContextFactory
{
    private readonly Dictionary<string, BranchDatabaseContext> _contexts = new();
    public async Task<BranchDatabaseContext> CreateAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsBuilder.BuildAsync(cancellationToken);
        return await CreateAsync(settings.BranchName, cancellationToken);
    }

    public async Task<BranchDatabaseContext> CreateAsync(string branchName, CancellationToken cancellationToken)
    {
        if (_contexts.TryGetValue(branchName, out var context))
            return context;

        var database = await databaseProvider.GetDatabaseAsync(branchName, cancellationToken);
        context = new BranchDatabaseContext(database);
        _contexts.Add(branchName, context);

        return context;
    }
}