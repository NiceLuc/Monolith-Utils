using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class SolutionFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _slnProjectsRegex = new(@"Project\(""\{(?<project_type_guid>.+?)\}""\).+?""(?<project_name>.+?)"".+?""(?<project_path>.+?\.(?<project_type>(cs|db|sql|wix))proj)"".+?""\{(?<project_guid>.+?)\}""", RegexOptions.Multiline);

    private readonly Dictionary<string, ProjectType> _projectTypes = new()
    {
        { "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", ProjectType.OldStyle },
        { "9A19103F-16F7-4668-BE54-9A1E7A4F7556", ProjectType.SdkStyle },
    };

    public async Task<Results> ScanAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileStorage.FileExists(path))
            throw new FileNotFoundException($"Solution file not found: {path}");

        var solutionDirectory = Path.GetDirectoryName(path)!;
        var solutionXml = await fileStorage.ReadAllTextAsync(path, cancellationToken);

        var results = new Results();

        foreach (Match match in _slnProjectsRegex.Matches(solutionXml))
        {
            var relativePath = Path.Combine(solutionDirectory, match.Groups["project_path"].Value);
            var projectPath = Path.GetFullPath(relativePath);

            var type = match.Groups["project_type"].Value;
            var guid = match.Groups["project_type_guid"].Value;

            switch (type)
            {
                case "wix":
                    results.WixProjects.Add(projectPath);
                    break;

                case "cs":

                    if (!_projectTypes.TryGetValue(guid, out var projectType))
                    {
                        results.Errors.Add($"Unknown project guid type. Solution: {path}, Project: {projectPath}, ProjectType: {guid}");
                        projectType = ProjectType.Unknown;
                    }

                    results.Projects.Add(new ProjectItem(projectPath, projectType));
                    break;

                case "db":
                case "sql":
                    results.Projects.Add(new ProjectItem(projectPath, ProjectType.Unknown));
                    break;

                default:
                    // regex pattern has no supporting switch block (this would be a developer bug)
                    throw new InvalidOperationException($"Unknown project type. Solution: {path}, Project: {projectPath}");
            }
        }

        return results;
    }

    public class Results
    {
        public List<string> Errors { get; } = new();
        public List<ProjectItem> Projects { get; } = new();
        public List<string> WixProjects { get; } = new();
    }

    public record struct ProjectItem(string Path, ProjectType Type);
}