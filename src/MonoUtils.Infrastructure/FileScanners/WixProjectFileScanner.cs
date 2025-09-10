using System.Text.RegularExpressions;
using MonoUtils.Domain;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _sdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
    private static readonly Regex _projectRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.Multiline);
    private static readonly Regex _wxsRegex = new(@"<Compile Include=""(?<wix_path>.+?\.wxs)""", RegexOptions.Multiline);

    public async Task<Results> ScanAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileStorage.FileExists(path))
            throw new FileNotFoundException($"WixProject file not found: {path}");

        var wixProjDirectory = Path.GetDirectoryName(path)!;
        var wixProjXml = await fileStorage.ReadAllTextAsync(path, cancellationToken);

        var packagesConfig = Path.Combine(wixProjDirectory, "packages.config");

        var results = new Results
        {
            IsSdk = _sdkRegex.IsMatch(wixProjXml),
            IsPackageRef = !fileStorage.FileExists(packagesConfig)
        };

        // capture csproj references
        foreach (Match match in _projectRegex.Matches(wixProjXml))
        {
            var relativePath = Path.Combine(wixProjDirectory, match.Groups["project_path"].Value);
            var projectPath = Path.GetFullPath(relativePath);
            results.ProjectReferences.Add(projectPath);
        }

        // capture wxs component files
        var wixDirectory = Path.GetDirectoryName(path)!;
        if (results.IsSdk)
        {
            results.ComponentFilePaths.AddRange(fileStorage.GetFilePaths(wixDirectory, "*.wxs"));
        }
        else
        {
            var wixProjectXml = await fileStorage.ReadAllTextAsync(path, cancellationToken);
            foreach (Match match in _wxsRegex.Matches(wixProjectXml))
            {
                var relativePath = Path.Combine(wixDirectory, match.Groups["wix_path"].Value);
                var wxsPath = Path.GetFullPath(relativePath);
                results.ComponentFilePaths.Add(wxsPath);
            }
        }

        return results;
    }

    public class Results
    {
        public bool IsSdk { get; set; }
        public bool IsPackageRef { get; set; }
        public List<string> ProjectReferences { get; } = new();
        public List<string> ComponentFilePaths { get; } = new();
    }

}