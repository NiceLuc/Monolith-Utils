using Microsoft.Extensions.Options;

namespace Delinq;

internal class ConfigSettingsBuilder(IOptions<AppSettings> appSettings, IContextConfigProvider provider) : IConfigSettingsBuilder
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public async Task<ConfigSettings> BuildAsync(string contextName, string branchName, CancellationToken cancellationToken)
    {
        var config = await provider.GetContextConfigAsync(contextName, branchName, cancellationToken);

        // temp directory settings
        var tempDirectory = _appSettings.TempDirectoryTemplate;
        var tempRepositoryDirectoryPath = Path.Combine(tempDirectory, _appSettings.RepoDirectoryName);
        var tempTestDirectoryPath = Path.Combine(tempDirectory, _appSettings.TestsDirectoryName);
        var tempMetaDataFilePath = Path.Combine(tempDirectory, _appSettings.MetaDataFileNameTemplate);
        var tempValidationFilePath = Path.Combine(tempDirectory, _appSettings.ValidationFileNameTemplate);
        var tempValidationReportFilePath = Path.Combine(tempDirectory, _appSettings.ValidationReportFileNameTemplate);

        // tfs directory settings
        var tfsDirectory = Path.Combine(_appSettings.TFSRootTemplate, config.SourceDirectoryPath);
        var tfsDbmlFilePath = Path.Combine(tfsDirectory, config.DbmlFileName);
        var tfsDesignerFilePath = Path.Combine(tfsDirectory, config.DesignerFileName);
        var tfsRepositoryFilePath = Path.Combine(tfsDirectory, config.RepositoryFileName);

        var settings = new ConfigSettings
        {
            TfsDbmlFilePath = Resolve(tfsDbmlFilePath),
            TfsDesignerFilePath = Resolve(tfsDesignerFilePath),
            TfsRepositoryFilePath = Resolve(tfsRepositoryFilePath),
            TempMetaDataFilePath = Resolve(tempMetaDataFilePath),
            TempValidationFilePath = Resolve(tempValidationFilePath),
            TempValidationReportFilePath = Resolve(tempValidationReportFilePath),
            TempRepositoryDirectoryPath = Resolve(tempRepositoryDirectoryPath),
            TempTestDirectoryPath = Resolve(tempTestDirectoryPath),
        };

        return settings;
        
        string Resolve(string pattern) => pattern
            .Replace("{{BRANCH_NAME}}", branchName)
            .Replace("{{CONTEXT_NAME}}", contextName);
    }
}