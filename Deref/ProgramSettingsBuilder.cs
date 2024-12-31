using Microsoft.Extensions.Options;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace Deref;

internal class ProgramSettingsBuilder(
    IFileStorage fileStorage,
    IDefinitionSerializer<ProgramConfig> configSerializer,
    IOptions<AppSettings> appSettings) : IProgramSettingsBuilder
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task<ProgramSettings> BuildAsync(CancellationToken cancellationToken)
    {
        var branchName = await ResolveBranchNameAsync(cancellationToken);
        return DoBuildAsync(branchName);
    }

    public Task<ProgramSettings> BuildAsync(string branchName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(branchName))
            throw new ArgumentNullException(nameof(branchName), "branchName is required");

        var settings = DoBuildAsync(branchName);
        return Task.FromResult(settings);
    }

    #region Private Methods

    private ProgramSettings DoBuildAsync(string branchName)
    {
        var tfsRootDirectory = _appSettings.GetTFSRootPath(branchName);
        var tempDirectory = _appSettings.GetTempPath(branchName);

        var settings = new ProgramSettings
        {
            BranchName = branchName,
            TfsRootDirectory = tfsRootDirectory,
            DirectoriesToIgnore = (from directory in _appSettings.DirectoriesToIgnore
                select Path.Combine(tfsRootDirectory, directory)).ToArray(),
            TempRootDirectory = tempDirectory,
            RequiredBuildSolutions = (from s in _appSettings.RequiredSolutions
                let path = Path.Combine(tfsRootDirectory, s.SolutionPath)
                let name = string.Format(s.BuildName, branchName)
                select new BuildDefinition(name, path, true)).ToArray()
        };

        return settings;
    }

    private async Task<string> ResolveBranchNameAsync(CancellationToken cancellationToken)
    {
        // if the config file has not been built for this branch, return the default branch name
        var configPath = Path.Combine(_appSettings.GetTempPath(), "config.json");
        if (!fileStorage.FileExists(configPath))
            return _appSettings.DefaultBranchName;

        // otherwise, open the config file and return the branch name in the file
        var config = await configSerializer.DeserializeAsync(configPath, cancellationToken);
        return string.IsNullOrEmpty(config.BranchName)
            ? _appSettings.DefaultBranchName
            : config.BranchName;
    }

    #endregion
}