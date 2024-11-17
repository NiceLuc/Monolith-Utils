using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
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

    public enum ProjectTypes { Sdk, Old, Directory, Wix }

    private class SolutionReference
    {
        public ProjectTypes ProjectType { get; set; }
        public string ProjectPath { get; set; }
    }

    public class Handler(
        ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IDefinitionSerializer<BranchDatabase> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        /*
         * Solution Patterns:
             * sdk style guid: 9A19103F-16F7-4668-BE54-9A1E7A4F7556
             * old style guid: FAE04EC0-301F-11D3-BF4B-00C04F79EFBC
             * directory guid: 2150E333-8FDC-42A3-9474-1A3956D46DE8
             * wix project guid: 930C7802-8A8C-48F9-8165-68863BCCD9DD

         * Project Patterns:
         *      assembly name: <AssemblyName>{{name}}</AssemblyName>
         *      sdk style: <Project Sdk="Microsoft.NET.Sdk">
         *      framework: <TargetFramework>netstandard2.0</TargetFramework>
         *      references: <ProjectReference Include="{{project file path}}" />

         * WixProj Patterns:
         *      wsx file: <Compile Include="{{wix file path}}" />
         *      project file: <ProjectReference Include="{{project file path}}" />
         *
         * Wxs Patterns:
         *      binary file: <File Source="$(var.API.TargetDir)inContact.Caching.dll" />
         */
        private static readonly Regex _slnProjectsRegex = new(@"Project\(""\{(?<project_type>.+?)\}""\).+?""(?<project_name>.+?)"".+?""(?<project_path>.+?\.(cs|wix)proj)"".+?""\{(?<project_guid>.+?)\}""", RegexOptions.Multiline);

        private static readonly Regex _csProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?)""", RegexOptions.Multiline);
        private static readonly Regex _csProjSdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
        private static readonly Regex _csProjNetStandardRegex = new(@"\<TargetFrameworks?\>.*netstandard2\.0.*\<\/TargetFrameworks?\>", RegexOptions.Multiline);

        private readonly Dictionary<string, ProjectTypes> _projectTypes = new()
        {
            { "9A19103F-16F7-4668-BE54-9A1E7A4F7556", ProjectTypes.Sdk },
            { "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", ProjectTypes.Old },
            { "2150E333-8FDC-42A3-9474-1A3956D46DE8", ProjectTypes.Directory },
            { "930C7802-8A8C-48F9-8165-68863BCCD9DD", ProjectTypes.Wix }
        };

        private readonly HashSet<string> _solutionNames = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly HashSet<string> _projectNames = new(StringComparer.InvariantCultureIgnoreCase);
        //private readonly HashSet<string> _wixProjNames = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<string, BranchDatabase.Solution> _solutions = new();
        private readonly Dictionary<string, BranchDatabase.Project> _projects = new();
        //private readonly Dictionary<string, BranchDatabase.WixProj> _wixProjects = new();

        private readonly Queue<string> _projectFilesToScan = new();
        //private readonly Queue<string> _wixProjFilesToScan = new();

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(request.BranchName, request.ResultsDirectoryPath, cancellationToken);
            ValidateRequest(settings, request);

            // reset all lists and dictionaries
            _projectFilesToScan.Clear();
            _solutionNames.Clear();
            _projectNames.Clear();
            _solutions.Clear();
            _projects.Clear();

            // scan all solution files and queue all project files for scanning
            foreach (var build in settings.BuildSolutions)
            {
                await ScanSolutionFileAsync(build.SolutionPath, (solution, references) =>
                {
                    solution.Builds.Add(build.BuildName);

                    foreach (var reference in references.Where(r => r.ProjectType != ProjectTypes.Directory))
                    {
                        // validate type here
                        if (reference.ProjectType == ProjectTypes.Wix)
                        {
                            // todo: scan for project references and wxs files to scan
                            // todo: loop through all project references and add wix name
                            // todo: queue wixproj files to scan
                            /*
                            var wixProj = GetOrAddWixProj(reference.ProjectPath, isHarvested: false);
                                            -> _wixProjFilesToScan.Enqueue(reference.ProjectPath);
                            wixProj.Solutions.Add(solution.Name);
                            solution.WixProjects.Add(wixProj.Name);
                             */
                            logger.LogWarning("TODO: " + reference.ProjectPath);
                            continue;
                        }

                        if (reference.ProjectType == ProjectTypes.Old || reference.ProjectType == ProjectTypes.Sdk)
                        {
                            var project = GetOrAddProject(reference.ProjectPath);
                            project.Solutions.Add(solution.Name);
                            solution.Projects.Add(project.Name);
                            continue;
                        }

                        throw new InvalidOperationException($"Solution reference not supported: {reference.ProjectType}");

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
            var data = new BranchDatabase
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
        
        private async Task ScanSolutionFileAsync(string solutionPath, Action<BranchDatabase.Solution, SolutionReference[]> callback, CancellationToken cancellationToken)
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

            var solutionItems = new List<SolutionReference>();
            foreach (Match match in _slnProjectsRegex.Matches(solutionFile))
            {
                var guid = match.Groups["project_type"].Value;
                if (!_projectTypes.TryGetValue(guid, out var type))
                {
                    logger.LogWarning("Project type not supported: " + match.Groups["project_path"].Value);
                    continue;
                }

                var relativePath = Path.Combine(solutionDirectory, match.Groups["project_path"].Value);
                var projectPath = Path.GetFullPath(relativePath);
                var reference = new SolutionReference
                {
                    ProjectType = type,
                    ProjectPath = projectPath
                };

                solutionItems.Add(reference);
            }

            // let the caller add build name and project references
            callback(solution, solutionItems.ToArray());
        }

        private async Task ScanProjectFileAsync(string projectPath, Action<BranchDatabase.Project, string[]> callback, CancellationToken cancellationToken)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
                throw new InvalidOperationException($"Project not in dictionary: {projectPath}");

            var projectXml = await fileStorage.ReadAllTextAsync(projectPath, cancellationToken);
            project.AssemblyName = GetAssemblyName(projectPath, projectXml);
            project.PdbFileName = GetPdbFileName(project.AssemblyName);
            project.IsSdk = _csProjSdkRegex.IsMatch(projectXml);
            project.IsNetStandard2 = _csProjNetStandardRegex.IsMatch(projectXml);

            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var packagesConfig = Path.Combine(projectDirectory, "packages.config");
            project.IsPackageRef = !fileStorage.FileExists(packagesConfig);

            var projectPaths = new List<string>();
            foreach (Match match in _csProjReferenceRegex.Matches(projectXml))
            {
                var relativePath = Path.Combine(projectDirectory, match.Groups["project_path"].Value);
                var referencePath = Path.GetFullPath(relativePath);
                projectPaths.Add(referencePath);
            }

            // let the caller add and manage project references
            callback(project, projectPaths.ToArray());
        }


        private BranchDatabase.Solution GetOrAddSolution(string solutionPath)
        {
            if (!_solutions.TryGetValue(solutionPath, out var solution))
            {
                var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
                solutionName = GetUniqueName(solutionName, _solutionNames.Contains);
                var exists = fileStorage.FileExists(solutionPath);
                solution = new BranchDatabase.Solution(solutionName, solutionPath, exists);
                _solutionNames.Add(solutionName);
                _solutions.Add(solutionPath, solution);
            }

            return solution;
        }

        private BranchDatabase.Project GetOrAddProject(string projectPath)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                projectName = GetUniqueName(projectName, _projectNames.Contains);
                var exists = fileStorage.FileExists(projectPath);
                project = new BranchDatabase.Project(projectName, projectPath, exists);
                _projectNames.Add(projectName);
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

        private static string GetAssemblyName(string projectFilePath, string projectXml)
        {
            // Define regex patterns
            const string assemblyNamePattern = @"<AssemblyName>(.*?)<\/AssemblyName>";
            const string outputTypePattern = @"<OutputType>(.*?)<\/OutputType>";

            // Match AssemblyName
            var assemblyNameMatch = Regex.Match(projectXml, assemblyNamePattern, RegexOptions.IgnoreCase);
            var assemblyName = assemblyNameMatch.Success
                ? assemblyNameMatch.Groups[1].Value
                : Path.GetFileNameWithoutExtension(projectFilePath);

            // Match OutputType
            var outputTypeMatch = Regex.Match(projectXml, outputTypePattern, RegexOptions.IgnoreCase);
            var outputType = outputTypeMatch.Success ? outputTypeMatch.Groups[1].Value : "Library";

            // Determine the file extension
            var extension = outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) ? ".exe" : ".dll";

            // Return the assembly name with the correct extension
            return $"{assemblyName}{extension}";
        }

        private static string GetPdbFileName(string assemblyName)
        {
            var lastIndex = assemblyName.LastIndexOf('.');
            if (lastIndex < 0)
                return assemblyName + ".pdb";

            return assemblyName[..lastIndex] + ".pdb";
        }
    }
}