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

    public Task<ProgramSettings> BuildAsync(CancellationToken cancellationToken) => BuildAsync(string.Empty, cancellationToken);

    public Task<ProgramSettings> BuildAsync(string branchName, CancellationToken cancellationToken) => BuildAsync(branchName, null, cancellationToken);

    public async Task<ProgramSettings> BuildAsync(string branchName, string? customTempDirectoryPath, CancellationToken cancellationToken)
    {
        branchName = await ResolveBranchNameAsync(branchName, cancellationToken);

        var tfsRootDirectory = ResolveDirectoryPath(_appSettings.TFSRootTemplate, branchName);
        var tempDirectory = ResolveDirectoryPath(string.IsNullOrEmpty(customTempDirectoryPath)
            ? _appSettings.TempDirectoryTemplate
            : customTempDirectoryPath, branchName);

        var settings = new ProgramSettings
        {
            BranchName = branchName,
            RootDirectory = tfsRootDirectory,
            TempDirectory = tempDirectory,
            BuildSolutions = (from s in _appSettings.RequiredSolutions
                let path = Path.Combine(tfsRootDirectory, s.SolutionPath)
                let name = string.Format(s.BuildName, branchName)
                select new BuildDefinition(name, path)).ToArray()
        };

        return settings;
    }


    private async Task<string> ResolveBranchNameAsync(string branchName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(branchName))
            return branchName;

        var rootPath = ResolveDirectoryPath(_appSettings.TempDirectoryTemplate, string.Empty);
        var configPath = Path.Combine(rootPath, "config.json");
        if (!fileStorage.FileExists(configPath))
            return _appSettings.DefaultBranchName;

        var config = await configSerializer.DeserializeAsync(configPath, cancellationToken);
        return config.BranchName;
    }

    private static string ResolveDirectoryPath(string pattern, string branchName) =>
        pattern.Replace("{{BRANCH_NAME}}", branchName);
}