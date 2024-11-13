using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharedKernel;

namespace Deref.Programs;

public class Initialize
{
    public class Request : IRequest<string>
    {
        public string BranchName { get; set; }
        public string ResultsDirectoryPath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(
        IProgramSettingsBuilder settingsBuilder,
        IDefinitionSerializer<BranchSchema> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        private static readonly Regex _csProjReferenceRegex = new(@"""(?<relative_path>[\.-\\a-zA-Z\d]+\.csproj)""", RegexOptions.Multiline);
        private static readonly Regex _projectSdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
        private static readonly Regex _projectNetStandardRegex = new(@"\<TargetFrameworks\>.*netstandard2\.0.*\<\/TargetFrameworks\>", RegexOptions.Multiline);

        private readonly Dictionary<string, BranchSchema.Solution> _solutions = new();
        private readonly Dictionary<string, BranchSchema.Project> _projects = new();
        private readonly Queue<string> _projectFilesToScan = new();


        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(request.BranchName, request.ResultsDirectoryPath, cancellationToken);
            ValidateRequest(settings, request);

            // reset all lists and dictionaries
            _projectFilesToScan.Clear();
            _solutions.Clear();
            _projects.Clear();

            // scan all solution files and queue all project files for scanning
            foreach (var build in settings.BuildSolutions)
            {
                await ScanSolutionFileAsync(build.SolutionPath, (solution, projectPaths) =>
                {
                    solution.Builds.Add(build.BuildName);

                    foreach (var projectPath in projectPaths)
                    {
                        var project = GetOrAddProject(projectPath);
                        project.Solutions.Add(solution.Name);
                        solution.Projects.Add(project.Name);
                    }
                }, cancellationToken);

            }

            // scan each project file one by one.
            // note: new project files may be added to the queue during a scan
            while (_projectFilesToScan.Count > 0)
            {
                var projectPath = _projectFilesToScan.Dequeue();
                await ScanProjectFileAsync(projectPath, (project, referencePaths) =>
                {
                    foreach (var referencePath in referencePaths)
                    {
                        var reference = GetOrAddProject(referencePath);
                        project.References.Add(reference.Name);
                        reference.ReferencedBy.Add(project.Name);
                    }
                }, cancellationToken);
            }

            // persist the results to a json file
            var data = new BranchSchema
            {
                Solutions = _solutions.Values.ToList(),
                Projects = _projects.Values.ToList(),
            };

            var filePath = Path.Combine(settings.TempDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return filePath;
        }


        private void ValidateRequest(ProgramSettings settings, Request request)
        {
            if (!fileStorage.DirectoryExists(settings.TempDirectory))
            {
                fileStorage.CreateDirectory(settings.TempDirectory);
                return;
            }

            if (!request.ForceOverwrite)
                throw new InvalidOperationException(
                    $"Results directory already exists: {settings.TempDirectory} (use -f to overwrite)");
        }
        
        private async Task ScanSolutionFileAsync(string solutionPath, Action<BranchSchema.Solution, string[]> callback, CancellationToken cancellationToken)
        {
            var solution = GetOrAddSolution(solutionPath);
            if (!solution.Exists)
            {
                // cannot scan a file that does not exist
                // but let the caller add build name references
                callback(solution, []);
                return;
            }

            var solutionFile = await fileStorage.ReadAllTextAsync(solutionPath, cancellationToken);
            var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

            var projectPaths = new List<string>();
            foreach (Match match in _csProjReferenceRegex.Matches(solutionFile))
            {
                var projectPath = Path.Combine(solutionDirectory, match.Groups["relative_path"].Value);
                projectPaths.Add(Path.GetFullPath(projectPath));
            }

            // let the caller add build name and project references
            callback(solution, projectPaths.ToArray());
        }

        private async Task ScanProjectFileAsync(string projectPath, Action<BranchSchema.Project, string[]> callback, CancellationToken cancellationToken)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
                throw new InvalidOperationException($"Project not in dictionary: {projectPath}");

            var projectFile = await fileStorage.ReadAllTextAsync(projectPath, cancellationToken);

            project.IsSdk = _projectSdkRegex.IsMatch(projectFile);
            project.IsNetStandard2 = _projectNetStandardRegex.IsMatch(projectFile);

            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var packagesConfig = Path.Combine(projectDirectory, "packages.config");
            project.IsPackageRef = !fileStorage.FileExists(packagesConfig);

            var projectPaths = new List<string>();
            foreach (Match match in _csProjReferenceRegex.Matches(projectFile))
            {
                var referencePath = Path.Combine(projectDirectory, match.Groups["relative_path"].Value);
                projectPaths.Add(Path.GetFullPath(referencePath));
            }

            // let the caller add and manage project references
            callback(project, projectPaths.ToArray());
        }

        private BranchSchema.Solution GetOrAddSolution(string solutionPath)
        {
            if (!_solutions.TryGetValue(solutionPath, out var solution))
            {
                var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
                solutionName = GetUniqueName(solutionName, _solutions.ContainsKey);
                var exists = fileStorage.FileExists(solutionPath);
                solution = new BranchSchema.Solution(solutionName, solutionPath, exists);
                _solutions.Add(solutionPath, solution);
            }

            return solution;
        }

        private BranchSchema.Project GetOrAddProject(string projectPath)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                projectName = GetUniqueName(projectName, _projects.ContainsKey);
                var exists = fileStorage.FileExists(projectPath);
                project = new BranchSchema.Project(projectName, projectPath, exists);
                _projects.Add(projectPath, project);

                // if the file exists, then we must queue the file to be scanned
                // later for any references it may have to other projects
                if (exists)
                    _projectFilesToScan.Enqueue(projectPath);
            }

            return project;
        }

        private static string GetUniqueName(string baseName, Func<string, bool> hasKey)
        {
            var offset = 0;

            var name = baseName;
            while (hasKey(name))
            {
                offset++;
                name = $"{baseName}-{offset}";
            }

            return name;
        }
    }
}