using Deref.Programs;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace Deref;

public class BranchDatabaseProvider(
    IProgramSettingsBuilder settingsBuilder, 
    IFileStorage fileStorage,
    IDefinitionSerializer<BranchDatabase> schemaSerializer) : IBranchDatabaseProvider
{
    public async Task<BranchDatabase> GetDatabaseAsync(string branchName, CancellationToken cancellationToken)
    {
        var settings = await settingsBuilder.BuildAsync(branchName, cancellationToken);

        var dbFilePath = Path.Combine(settings.TempDirectory, "db.json");
        if (!fileStorage.FileExists(dbFilePath))
            throw new FileNotFoundException($"File not found: {dbFilePath}");

        return await schemaSerializer.DeserializeAsync(dbFilePath, cancellationToken);
    }
}

internal static class BranchDatabaseExtensions
{
    internal static QueryOptions ToQueryOptions(this IQueryRequest request)
    {
        return new QueryOptions
        {
            BranchFilter = request.BranchFilter,
            SearchTerm = request.SearchTerm,
            IsExcludeTests = request.IsExcludeTests,
            IsRecursive = request.IsRecursive,
            ShowListCounts = request.ShowListCounts,
            ShowListTodos = request.ShowListTodos
        };
    }
}