using Microsoft.Extensions.Options;

namespace Deref;

internal class ProgramSettingsBuilder(IOptions<AppSettings> appSettings) : IProgramSettingsBuilder
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public ProgramSettings Build(string branchName, string customTempDirectoryPath)
    {
        branchName = string.IsNullOrEmpty(branchName) ? _appSettings.DefaultBranchName : branchName;

        var tfsRootDirectory = Resolve(_appSettings.TFSRootTemplate);
        var tempDirectory = Resolve(string.IsNullOrEmpty(customTempDirectoryPath)
            ? _appSettings.TempDirectoryTemplate
            : customTempDirectoryPath);

        var metaPathTemplate = Path.Combine(tempDirectory, _appSettings.MetaDataFileNameTemplate);

        var settings = new ProgramSettings
        {
            RootDirectory = tfsRootDirectory,
            TempDirectory = tempDirectory,
            BuildSolutions = (from s in _appSettings.RequiredSolutions
                             let path = Path.Combine(tfsRootDirectory, s.SolutionPath)
                             let name = Path.GetFileNameWithoutExtension(path)
                             let meta = ResolveMeta(name)
                             select new BuildDefinition
                             {
                                 BuildName = s.BuildName,
                                 SolutionPath = path,
                                 MetaDataPath = meta
                             }).ToArray()
        };

        return settings;
        
        string Resolve(string pattern) => pattern
            .Replace("{{BRANCH_NAME}}", branchName);

        string ResolveMeta(string name) => Resolve(metaPathTemplate)
            .Replace("{{SOLUTION_NAME}}", name)
            .Replace(" ", "_");
    }
}