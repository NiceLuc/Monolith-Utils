using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        DefinitionSerializer<BranchSchema> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        private static readonly Regex _referenceRegex =
            new(@"""(?<relative_path>[\.-\\a-zA-Z\d]+\.csproj)""", RegexOptions.Multiline);

        private readonly Dictionary<string, SolutionToken> _solutions = new();
        private readonly Dictionary<string, ProjectToken> _projects = new();
        private readonly Queue<string> _projectFilesToScan = new();


        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = settingsBuilder.Build(request.BranchName, request.ResultsDirectoryPath);
            ValidateRequest(settings, request);

            // reset all lists and dictionaries
            _solutions.Clear();
            _projects.Clear();
            _projectFilesToScan.Clear();

            var solutionPaths = settings.BuildSolutions.Select(s
                    => Path.Combine(settings.RootDirectory, s.SolutionPath))
                .Distinct() // some builds are using the same solution file (ie. MVIM2 & UpdateMaxVersionInstaller)
                .Where(path => File.Exists(path!)); // some local branch cloak large directories to save space!

            // add all solutions to the builder
            foreach (var solutionPath in solutionPaths)
            {
                await AddSolutionAsync(solutionPath, (solution, projectPaths) =>
                {
                    foreach (var projectPath in projectPaths)
                    {
                        var project = GetOrAddProject(projectPath);
                        project.Solutions.Add(solution.Name);
                    }
                }, cancellationToken);
            }

            while (_projectFilesToScan.Count > 0)
            {
                var projectPath = _projectFilesToScan.Dequeue();
                await AddProjectAsync(projectPath, (project, referencePaths) =>
                {
                    foreach (var referencePath in referencePaths)
                    {
                        var reference = GetOrAddProject(referencePath);
                        project.References.Add(reference.Name);
                        reference.ReferencedBy.Add(project.Name);
                    }
                }, cancellationToken);
            }

            var data = Build();
            var filePath = Path.Combine(settings.TempDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return $"Database contains {data.Projects.Count} project";
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

        private async Task AddSolutionAsync(string solutionPath, Action<SolutionToken, string[]> callback, CancellationToken cancellationToken)
        {
            if (_solutions.ContainsKey(solutionPath))
                return; // already scanned!

            var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            solutionName = GetUniqueName(solutionName, _solutions.ContainsKey);
            var exists = File.Exists(solutionPath);
            var token = new SolutionToken(solutionName, solutionPath, exists);
            _solutions.Add(solutionPath, token);

            var solutionFile = await fileStorage.ReadAllTextAsync(solutionPath, cancellationToken);
            var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

            var projectPaths = new List<string>();
            foreach (Match match in _referenceRegex.Matches(solutionFile))
            {
                var projectPath = Path.Combine(solutionDirectory, match.Groups["relative_path"].Value);
                projectPaths.Add(Path.GetFullPath(projectPath));
            }

            // let the caller add the projects to the builder
            callback(token, projectPaths.ToArray());
        }

        private async Task AddProjectAsync(string projectPath, Action<ProjectToken, string[]> callback, CancellationToken cancellationToken)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
                throw new InvalidOperationException($"Project not in dictionary: {projectPath}");

            var projectFile = await fileStorage.ReadAllTextAsync(projectPath, cancellationToken);
            var projectDirectory = Path.GetDirectoryName(projectPath)!;

            var projectPaths = new List<string>();
            foreach (Match match in _referenceRegex.Matches(projectFile))
            {
                var referencePath = Path.Combine(projectDirectory, match.Groups["relative_path"].Value);
                projectPaths.Add(Path.GetFullPath(referencePath));
            }

            // let the caller add the projects to the builder
            callback(project, projectPaths.ToArray());
        }

        private ProjectToken GetOrAddProject(string projectPath)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                projectName = GetUniqueName(projectName, _projects.ContainsKey);
                var exists = File.Exists(projectPath);
                project = new ProjectToken(projectName, projectPath, exists);
                _projects.Add(projectPath, project);

                if (exists)
                    _projectFilesToScan.Enqueue(projectPath);
            }

            return project;
        }

        private BranchSchema Build()
        {
            var projects = _projects.Values.Select(project =>
                new BranchSchema.Project
                {
                    Name = project.Name,
                    Path = project.Path,
                    Exists = project.Exists,
                    Solutions = project.Solutions,
                    ReferencedBy = project.ReferencedBy,
                    References = project.References,
                });

            var solutions = _solutions.Values.Select(solution => 
                new BranchSchema.Solution
                {
                    Name = solution.Name,
                    Path = solution.Path,
                    Exists = solution.Exists,
                    Projects = solution.Projects,
                });

            return new BranchSchema
            {
                Solutions = solutions.ToList(),
                Projects = projects.ToList(),
                // ProjectReferences = references.ToList()
            };
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

    private record ProjectToken(string Name, string Path, bool Exists)
    {
        public List<string> Solutions { get; } = new();
        public List<string> References { get; } = new();
        public List<string> ReferencedBy { get; } = new();
    }

    private record SolutionToken(string Name, string Path, bool Exists)
    {
        public List<string> Projects { get; } = new();
    }

}