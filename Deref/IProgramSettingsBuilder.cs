namespace Deref;

public interface IProgramSettingsBuilder
{
    Task<ProgramSettings> BuildAsync(string branchName, string? customTempDirectoryPath, CancellationToken cancellationToken);
}