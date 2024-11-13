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