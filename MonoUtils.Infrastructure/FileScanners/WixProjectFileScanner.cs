using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _sdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
    private static readonly Regex _projectRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.Multiline);

    public async Task ScanAsync(BranchDatabaseBuilder builder, SolutionRecord solution, WixProjectRecord wixProject, CancellationToken cancellationToken)
    {
        var wixProjDirectory = Path.GetDirectoryName(wixProject.Path)!;

        // capture all wix project references
        var wixProjXml = await fileStorage.ReadAllTextAsync(wixProject.Path, cancellationToken);

        wixProject.IsSdk = _sdkRegex.IsMatch(wixProjXml);

        // capture all cs project references
        foreach (Match match in _projectRegex.Matches(wixProjXml))
        {
            var relativePath = Path.Combine(wixProjDirectory, match.Groups["project_path"].Value);
            var projectPath = Path.GetFullPath(relativePath);

            // get a reference to the csharp project
            var project = builder.GetOrAddProject(projectPath, solution.IsRequired);

            // csharp project is required for the wix project (not harvested)
            var wixReference = new WixProjectReference(wixProject.Name, false);
            project.WixProjects.Add(wixReference);

            // wix project depends on the csharp project (not harvested)
            var projectReference = new WixProjectReference(project.Name, false);
            wixProject.ProjectReferences.Add(projectReference);
        }
    }
}