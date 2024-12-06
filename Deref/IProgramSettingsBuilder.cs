namespace Deref;

public interface IProgramSettingsBuilder
{
    Task<ProgramSettings> BuildAsync(CancellationToken cancellationToken);
    Task<ProgramSettings> BuildAsync(string branchName, CancellationToken cancellationToken);
}