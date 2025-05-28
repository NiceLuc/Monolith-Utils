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

    public async Task<Results> ScanAsync(SolutionRecord solution, CancellationToken cancellationToken)
    {
        var results = new Results(solution);

        // cannot scan a file that does not exist
        if (!solution.DoesExist)
            return results;

        var solutionDirectory = Path.GetDirectoryName(solution.Path)!;
        var solutionXml = await fileStorage.ReadAllTextAsync(solution.Path, cancellationToken);
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
                        solution.Errors.Add($"Unknown project guid type. Solution: {solution.Path}, Project: {projectPath}, ProjectType: {guid}");
                        projectType = ProjectType.Unknown;
                    }

                    results.Projects.Add(new ProjectItem(projectPath, projectType));
                    break;

                default:
                    solution.Errors.Add($"Unknown project type. Solution: {solution.Path}, Project: {projectPath}");
                    break;
            }
        }

        // let the caller add build name and project references
        return results;
    }

    public class Results(SolutionRecord solution)
    {
        public SolutionRecord Solution { get; } = solution;
        public List<ProjectItem> Projects { get; } = new();
        public List<string> WixProjects { get; } = new();
    }

    public record ProjectItem(string Path, ProjectType Type);
}