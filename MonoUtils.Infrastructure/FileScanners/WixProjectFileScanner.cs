using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _sdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
    private static readonly Regex _projectRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.Multiline);
    private static readonly Regex _wxsRegex = new(@"<Compile Include=""(?<wix_path>.+?\.wxs)""", RegexOptions.Multiline);

    public async Task<WixProjectScanResults> ScanAsync(WixProjectRecord wixProject, CancellationToken cancellationToken)
    {
        // capture all wix project references
        var wixProjDirectory = Path.GetDirectoryName(wixProject.Path)!;
        var wixProjXml = await fileStorage.ReadAllTextAsync(wixProject.Path, cancellationToken);
        wixProject.IsSdk = _sdkRegex.IsMatch(wixProjXml);

        // capture all cs project references
        var results = new WixProjectScanResults(wixProject);
        foreach (Match match in _projectRegex.Matches(wixProjXml))
        {
            var relativePath = Path.Combine(wixProjDirectory, match.Groups["project_path"].Value);
            var projectPath = Path.GetFullPath(relativePath);
            results.ProjectReferences.Add(projectPath);
        }

        var wxsFiles = await GetWxsFilePaths(wixProject, cancellationToken);
        foreach (var wxsFile in wxsFiles) 
            results.ComponentFilePaths.Add(wxsFile);

        return results;
    }

    private async Task<List<string>> GetWxsFilePaths(WixProjectRecord wixProject, CancellationToken cancellationToken)
    {
        var wixDirectory = Path.GetDirectoryName(wixProject.Path)!;
        if (wixProject.IsSdk)
            return fileStorage.GetFilePaths(wixDirectory, "*.wxs").ToList();

        var wixProjectXml = await fileStorage.ReadAllTextAsync(wixProject.Path, cancellationToken);
        var wxsFilePaths = new List<string>();
        foreach (Match match in _wxsRegex.Matches(wixProjectXml))
        {
            var relativePath = Path.Combine(wixDirectory, match.Groups["wix_path"].Value);
            var wxsPath = Path.GetFullPath(relativePath);
            wxsFilePaths.Add(wxsPath);
        }

        return wxsFilePaths;
    }

    public class WixProjectScanResults(WixProjectRecord wixProject)
    {
        public WixProjectRecord WixProject { get; } = wixProject;
        public List<string> ProjectReferences { get; } = new();
        public List<string> ComponentFilePaths { get; } = new();
    }

}