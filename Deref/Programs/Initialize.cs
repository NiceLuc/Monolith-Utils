using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

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
        ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IDefinitionSerializer<BranchDatabase> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        private static readonly Regex _slnProjectsRegex = new(@"Project\(""\{(?<project_type>.+?)\}""\).+?""(?<project_name>.+?)"".+?""(?<project_path>.+?\.(cs|wix)proj)"".+?""\{(?<project_guid>.+?)\}""", RegexOptions.Multiline);

        private static readonly Regex _csProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.Multiline);
        private static readonly Regex _csProjSdkRegex = new(@"<Project Sdk=", RegexOptions.Multiline);
        private static readonly Regex _csProjNetStandardRegex = new(@"\<TargetFrameworks?\>.*netstandard2\.0.*\<\/TargetFrameworks?\>", RegexOptions.Multiline);

        private static readonly Regex _wixProjWixProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.wixproj)""", RegexOptions.Multiline);
        private static readonly Regex _wixProjWxsReferenceRegex = new(@"<Compile Include=""(?<wix_path>.+?\.wxs)""", RegexOptions.Multiline);
        private static readonly Regex _wxsAssemblyNameRegex = new(@"File.+?Source=""\$\(.+?\)(?<assembly_name>.+?\.dll)""", RegexOptions.Multiline);

        private readonly Dictionary<string, ProjectTypes> _projectTypes = new()
        {
            { "9A19103F-16F7-4668-BE54-9A1E7A4F7556", ProjectTypes.Csharp },
            { "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", ProjectTypes.Csharp },
            { "930C7802-8A8C-48F9-8165-68863BCCD9DD", ProjectTypes.Wix }
        };

        private readonly HashSet<string> _solutionNames = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly HashSet<string> _projectNames = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly HashSet<string> _wixProjNames = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<string, SolutionRecord> _solutions = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, ProjectRecord> _projects = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, WixProjectRecord> _wixProjects = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly Queue<string> _wixProjFilesToPreScan = new();
        private readonly Queue<string> _projectFilesToScan = new();
        private readonly Queue<string> _wixProjFilesToScan = new();


        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(request.BranchName, request.ResultsDirectoryPath, cancellationToken);
            ValidateRequest(settings, request);

            ResetLists();

            // scan all solution files and queue all project files for scanning
            foreach (var build in settings.BuildSolutions)
            {
                await ScanSolutionFileAsync(build.SolutionPath, true, (solution, references) =>
                {
                    solution.Builds.Add(build.BuildName);

                    foreach (var reference in references)
                    {
                        if (reference.ProjectType == ProjectTypes.Wix)
                        {
                            // we only want to look for other wix projects at first
                            // our final scan comes after all projects are scanned
                            // in order to lookup assembly name references!
                            var wixProj = GetOrAddWixProj(reference.ProjectPath, true, true);
                            wixProj.Solutions.Add(solution.Name);
                            solution.WixProjects.Add(wixProj.Name);
                            continue;
                        }

                        if (reference.ProjectType == ProjectTypes.Csharp)
                        {
                            var project = GetOrAddProject(reference.ProjectPath, true);
                            project.Solutions.Add(solution.Name);
                            solution.Projects.Add(project.Name);
                            continue;
                        }

                        throw new InvalidOperationException($"Solution reference not supported: {reference.ProjectType}");
                    }
                }, cancellationToken);
            }

            // pre-scan all wix project files for nested wix project references 
            // capture all project files that are not harvested from the project file
            while (_wixProjFilesToPreScan.Count > 0)
            {
                var wixProjectPath = _wixProjFilesToPreScan.Dequeue();
                await PreScanWixProjectFileAsync(wixProjectPath, (wixProject, references) =>
                {
                    foreach (var reference in references)
                    {
                        if (reference.ProjectType == ProjectTypes.Wix)
                        {
                            var wixRef = GetOrAddWixProj(reference.ProjectPath, true, true);
                            wixProject.References.Add(wixRef.Name);
                            wixRef.ReferencedBy.Add(wixProject.Name);
                            continue;
                        }

                        if (reference.ProjectType == ProjectTypes.Csharp)
                        {
                            // we just want to add the project to our list to be scanned
                            // we don't set any references yet
                            GetOrAddProject(reference.ProjectPath, true);
                            continue;
                        }

                        throw new InvalidOperationException($"Wix project reference not supported: {reference.ProjectType}");
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
                        var reference = GetOrAddProject(referencePath, true);
                        project.References.Add(reference.Name);
                        reference.ReferencedBy.Add(project.Name);
                    }
                }, cancellationToken);
            }

            // scan each wix project file one by one.
            // note: we provide the assembly  names to scan our wxs files for harvested binaries
            var assemblyNames = _projects.Values
                .Where(p => p.DoesExist) // file must exist!
                .Where(p => !p.Path.Contains("Test", StringComparison.OrdinalIgnoreCase)) // don't want test projects
                .ToDictionary(p => p.AssemblyName, p => p.Name);

            // useful for looking up projects by name (rather than by path)
            var projectsByName = _projects.Values.ToDictionary(p => p.Name);

            while (_wixProjFilesToScan.Count > 0)
            {
                var wixProjectPath = _wixProjFilesToScan.Dequeue();

                await ScanWixProjectFileAsync(wixProjectPath, assemblyNames, (wixProject, references) =>
                {
                    foreach (var reference in references)
                    {
                        if (!projectsByName.TryGetValue(reference.ProjectName, out var project))
                        {
                            logger.LogWarning("Wix project reference not found: " + reference);
                            continue;
                        }

                        // add a reference to the project from the wix entry
                        var wixReference = new WixProjectReference(project.Name, reference.IsHarvested);
                        wixProject.ProjectReferences.Add(wixReference);

                        // add a reference to the wix entry from the project
                        var projectReference = new WixProjectReference(wixProject.Name, reference.IsHarvested);
                        project.WixProjects.Add(projectReference);
                    }
                }, cancellationToken);
            }

            // persist the results to a json file
            var data = new BranchDatabase
            {
                Solutions = _solutions.Values.ToList(),
                Projects = _projects.Values.ToList(),
                WixProjects = _wixProjects.Values.ToList()
            };

            var filePath = Path.Combine(settings.TempDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return filePath;
        }

        private void ResetLists()
        {
            _wixProjFilesToPreScan.Clear();
            _projectFilesToScan.Clear();
            _wixProjFilesToScan.Clear();

            _solutionNames.Clear();
            _projectNames.Clear();
            _wixProjNames.Clear();

            _solutions.Clear();
            _projects.Clear();
            _wixProjects.Clear();
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

        private async Task ScanSolutionFileAsync(string solutionPath, bool isRequired,
            Action<SolutionRecord, ProjectReference[]> callback, CancellationToken cancellationToken)
        {
            var solution = GetOrAddSolution(solutionPath, isRequired);
            if (!solution.DoesExist)
            {
                // cannot scan a file that does not exist
                // but let the caller add build name references
                callback(solution, []);
                return;
            }

            var solutionFile = await fileStorage.ReadAllTextAsync(solutionPath, cancellationToken);
            var solutionDirectory = Path.GetDirectoryName(solutionPath)!;

            var solutionItems = new List<ProjectReference>();
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
                var reference = new ProjectReference(type, projectPath);
                solutionItems.Add(reference);
            }

            // let the caller add build name and project references
            callback(solution, solutionItems.ToArray());
        }

        private async Task PreScanWixProjectFileAsync(string wixProjectPath, Action<WixProjectRecord, ProjectReference[]> callback, CancellationToken cancellationToken)
        {
            if (!_wixProjects.TryGetValue(wixProjectPath, out var wixProject))
                throw new InvalidOperationException($"Wix project not in dictionary: {wixProjectPath}");

            var wixProjDirectory = Path.GetDirectoryName(wixProjectPath)!;
            var wixProjXml = await fileStorage.ReadAllTextAsync(wixProjectPath, cancellationToken);

            var referencePaths = new List<ProjectReference>();

            // capture all wix project references
            foreach (Match match in _wixProjWixProjReferenceRegex.Matches(wixProjXml))
            {
                var relativePath = Path.Combine(wixProjDirectory, match.Groups["project_path"].Value);
                var projectPath = Path.GetFullPath(relativePath);

                var reference = new ProjectReference(ProjectTypes.Wix, projectPath);
                referencePaths.Add(reference);
            }

            // capture all cs project references
            foreach (Match match in _csProjReferenceRegex.Matches(wixProjXml))
            {
                var relativePath = Path.Combine(wixProjDirectory, match.Groups["project_path"].Value);
                var projectPath = Path.GetFullPath(relativePath);

                var reference = new ProjectReference(ProjectTypes.Csharp, projectPath);
                referencePaths.Add(reference);
            }

            callback(wixProject, referencePaths.ToArray());
        }

        private async Task ScanProjectFileAsync(string projectPath, Action<ProjectRecord, string[]> callback, CancellationToken cancellationToken)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
                throw new InvalidOperationException($"Project not in dictionary: {projectPath}");

            var projectXml = await fileStorage.ReadAllTextAsync(projectPath, cancellationToken); // todo: ignore comments!!
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

        private async Task ScanWixProjectFileAsync(string wixProjectFilePath, Dictionary<string, string> assemblyNames,
            Action<WixProjectRecord, WixReference[]> callback, CancellationToken cancellationToken)
        {
            if (!_wixProjects.TryGetValue(wixProjectFilePath, out var wixProject))
                throw new InvalidOperationException($"Wix project not in dictionary: {wixProjectFilePath}");

            var wixProjXml = await fileStorage.ReadAllTextAsync(wixProjectFilePath, cancellationToken);
            wixProject.IsSdk = _csProjSdkRegex.IsMatch(wixProjXml);

            var projectDirectory = Path.GetDirectoryName(wixProjectFilePath)!;
            var packagesConfig = Path.Combine(projectDirectory, "packages.config");
            wixProject.IsPackageRef = !fileStorage.FileExists(packagesConfig);

            var references = new List<WixReference>();

            // capture all referenced projects in the wix project file
            foreach (Match match in _csProjReferenceRegex.Matches(wixProjXml))
            {
                var relativePath = Path.Combine(projectDirectory, match.Groups["project_path"].Value);
                var projectPath = Path.GetFullPath(relativePath);
                if (!_projects.TryGetValue(projectPath, out var project))
                {
                    logger.LogError($"Wix project reference not found: {projectPath}");
                    continue;
                }

                var reference = new WixReference(project.Name, false);
                references.Add(reference);
            }

            // find all harvested projects associated with this wix project file
            var wxsFilePaths = new List<string>();
            if (wixProject.IsSdk)
            {
                wxsFilePaths.AddRange(fileStorage.GetFilePaths(projectDirectory, "*.wxs"));
            }
            else
            {
                foreach (Match match in _wixProjWxsReferenceRegex.Matches(wixProjXml))
                {
                    var relativePath = Path.Combine(projectDirectory, match.Groups["wix_path"].Value);
                    var wxsPath = Path.GetFullPath(relativePath);
                    wxsFilePaths.Add(wxsPath);
                }
            }

            foreach (var wxsFilePath in wxsFilePaths)
            {
                var wxsXml = await fileStorage.ReadAllTextAsync(wxsFilePath, cancellationToken);
                foreach (Match match in _wxsAssemblyNameRegex.Matches(wxsXml))
                {
                    var assemblyName = match.Groups["assembly_name"].Value;
                    var assemblyNameIndex = assemblyName.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
                    if (assemblyNameIndex >= 0)
                        assemblyName = assemblyName[(assemblyNameIndex + 1)..];

                    // this is not a project reference, but a harvested binary reference
                    if (!assemblyNames.TryGetValue(assemblyName, out var projectName))
                        continue;

                    var reference = new WixReference(projectName, true);
                    references.Add(reference);
                }
            }

            // let the caller add and manage project references
            callback(wixProject, references.ToArray());
        }


        private SolutionRecord GetOrAddSolution(string solutionPath, bool isRequired)
        {
            if (!_solutions.TryGetValue(solutionPath, out var solution))
            {
                var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
                solutionName = GetUniqueName(solutionName, _solutionNames.Contains);
                var exists = fileStorage.FileExists(solutionPath);
                solution = new SolutionRecord(solutionName, solutionPath, isRequired, exists);
                _solutionNames.Add(solutionName);
                _solutions.Add(solutionPath, solution);
            }

            return solution;
        }

        private ProjectRecord GetOrAddProject(string projectPath, bool isRequired)
        {
            if (!_projects.TryGetValue(projectPath, out var project))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                projectName = GetUniqueName(projectName, _projectNames.Contains);
                var exists = fileStorage.FileExists(projectPath);
                project = new ProjectRecord(projectName, projectPath, isRequired, exists);
                _projectNames.Add(projectName);
                _projects.Add(projectPath, project);

                // if the file exists, then we must queue the file to be scanned
                // later for any references it may have to other projects
                if (exists)
                    _projectFilesToScan.Enqueue(projectPath);
            }

            return project;
        }

        private WixProjectRecord GetOrAddWixProj(string projectPath, bool isRequired, bool isPreScan)
        {
            if (!_wixProjects.TryGetValue(projectPath, out var project))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                projectName = GetUniqueName(projectName, _wixProjNames.Contains);
                var exists = fileStorage.FileExists(projectPath);
                project = new WixProjectRecord(projectName, projectPath, isRequired, exists);
                _wixProjNames.Add(projectName);
                _wixProjects.Add(projectPath, project);

                // if the file exists, then we must queue the file to be scanned
                // later for any references it may have to wxs files and other cs projects
                if (exists)
                {
                    if (isPreScan)
                        _wixProjFilesToPreScan.Enqueue(projectPath);

                    _wixProjFilesToScan.Enqueue(projectPath);
                }
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

    private enum ProjectTypes { Csharp, Wix }
    private record ProjectReference(ProjectTypes ProjectType, string ProjectPath);
    private record WixReference(string ProjectName, bool IsHarvested);
}