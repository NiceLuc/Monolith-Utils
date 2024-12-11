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

    public async Task<Results> ScanAsync(BranchDatabaseBuilder builder, string filePath, CancellationToken cancellationToken)
    {
        var solution = builder.GetOrAddSolution(filePath);
        var results = new Results(solution);

        // cannot scan a file that does not exist
        if (!solution.DoesExist)
            return results;

        var solutionDirectory = Path.GetDirectoryName(filePath)!;

        var solutionXml = await fileStorage.ReadAllTextAsync(filePath, cancellationToken);
        foreach (Match match in _slnProjectsRegex.Matches(solutionXml))
        {
            var relativePath = Path.Combine(solutionDirectory, match.Groups["project_path"].Value);
            var projectPath = Path.GetFullPath(relativePath);

            var type = match.Groups["project_type"].Value;

            // wix files get scanned outside of this scanner method. they are pre-scanned before
            // all other projects are scanned, but are then scanned again at the end to determine
            // if any component files reference them for manual harvesting
            if (type == "wix")
            {
                var wixProject = builder.GetOrAddWixProject(projectPath, solution.IsRequired);
                wixProject.Solutions.Add(solution.Name);
                solution.WixProjects.Add(wixProject.Name);

                // wix projects get returned to the caller
                results.WixProjectsToScan.Add(wixProject);
                continue;
            }

            // standard project files get added with a reference to their project type
            var project = builder.GetOrAddProject(projectPath, solution.IsRequired);
            var guid = match.Groups["project_type_guid"].Value;
            var projectType = GetProjectType(project, type, guid);
            var reference = new ProjectReference(project.Name, projectType);
            project.Solutions.Add(solution.Name);
            solution.Projects.Add(reference);
        }

        // let the caller add build name and project references
        return results;

        ProjectType GetProjectType(ProjectRecord project, string extensionType, string projectTypeGuid)
        {
            if (extensionType != "cs")
                return ProjectType.Unknown;

            if(_projectTypes.TryGetValue(projectTypeGuid, out var projectType))
                return projectType;

            builder.AddError($"Unknown csproj type. Solution: {solution.Path}, Project: {project.Path}, ProjectType: {projectTypeGuid}");
            return ProjectType.Unknown;
        }
    }

    public class Results(SolutionRecord solution)
    {
        public SolutionRecord Solution { get; } = solution;
        public List<WixProjectRecord> WixProjectsToScan { get; set; } = new();
    }
}