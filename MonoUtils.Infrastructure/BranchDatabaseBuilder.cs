using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilder(ILoggerFactory loggerFactory, IFileStorage fileStorage, UniqueNameResolver resolver, BuildDefinition[] builds)
{
    private static readonly Regex _testProjectFilePathRegex = new(@"Tests?\.csproj", RegexOptions.Multiline);

    private readonly ILogger<BranchDatabaseBuilder> logger = loggerFactory.CreateLogger<BranchDatabaseBuilder>();

    // required solutions (i.e. build definitions have a reference to a solution path)
    private readonly Dictionary<string, BuildDefinition> _requiredSolutions = builds.ToDictionary(s => s.SolutionPath);

    // record name & record
    private readonly Dictionary<string, SolutionRecord> _solutionsByName = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, ProjectRecord> _projectsByName = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, WixProjectRecord> _wixProjectsByName = new(StringComparer.InvariantCultureIgnoreCase);

    // file path & record
    private readonly Dictionary<string, SolutionRecord> _solutionsByPath = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, ProjectRecord> _projectsByPath = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, WixProjectRecord> _wixProjectsByPath = new(StringComparer.InvariantCultureIgnoreCase);

    // references
    private readonly List<SolutionProjectReference> _solutionReferences = new();
    private readonly List<ProjectReference> _projectReferences = new();
    private readonly List<SolutionWixProjectReference> _solutionInstallers = new();
    private readonly List<WixProjectReference> _wixProjectReferences = new();
    private readonly List<WixAssemblyReference> _wixAssemblyReferences = new();

    // files to scan
    private readonly Queue<ProjectRecord> _projectFilesToScan = new();
    private readonly Queue<WixProjectRecord> _wixProjectFilesToScan = new();

    private readonly List<string> _errors = new();

    public void AddError(string error) => _errors.Add(error);

    public SolutionRecord GetOrAddSolution(string solutionPath)
    {
        if (_solutionsByPath.TryGetValue(solutionPath, out var solution))
            return solution;

        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        solutionName = resolver.GetUniqueName(solutionName, _solutionsByName.ContainsKey);
        var isRequired = _requiredSolutions.TryGetValue(solutionPath, out var build);
        var exists = fileStorage.FileExists(solutionPath);

        solution = new SolutionRecord(solutionName, solutionPath, exists, isRequired);
        if (build is not null)
            solution.Builds.Add(build.BuildName);

        _solutionsByName.Add(solutionName, solution);
        _solutionsByPath.Add(solutionPath, solution);

        return solution;
    }

    public ProjectRecord GetOrAddProject(string projectPath)
    {
        if (_projectsByPath.TryGetValue(projectPath, out var project))
            return project;

        // otherwise, create a new wix project and set initial references
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        projectName = resolver.GetUniqueName(projectName, _projectsByName.ContainsKey);
        var exists = fileStorage.FileExists(projectPath);

        project = new ProjectRecord(projectName, projectPath, exists);

        _projectsByName.Add(projectName, project);
        _projectsByPath.Add(projectPath, project);

        // if the file exists, then we must queue the file to be scanned
        if (exists)
            _projectFilesToScan.Enqueue(project);

        return project;
    }

    public WixProjectRecord GetOrAddWixProject(string projectPath)
    {
        if (_wixProjectsByPath.TryGetValue(projectPath, out var project))
            return project;

        // otherwise, create a new wix project and set initial references
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        projectName = resolver.GetUniqueName(projectName, _wixProjectsByName.ContainsKey);
        var exists = fileStorage.FileExists(projectPath);

        project = new WixProjectRecord(projectName, projectPath, exists);

        _wixProjectsByName.Add(projectName, project);
        _wixProjectsByPath.Add(projectPath, project);

        // if the file exists, then we must queue the file to be scanned at a later time (once all cs projects have been scanned)
        // this is required to determine if the assembly name is referenced in any wxs files (i.e. manual binary harvesting)
        if (exists)
            _wixProjectFilesToScan.Enqueue(project);

        return project;
    }

    public void AddSolutionProject(SolutionRecord solution, ProjectRecord project, ProjectType type) 
        => _solutionReferences.Add(new SolutionProjectReference(solution.Name, project.Name, type));

    public void AddProjectReference(ProjectRecord project, ProjectRecord reference) 
        => _projectReferences.Add(new ProjectReference(project.Name, reference.Name));

    public void AddSolutionWixProject(SolutionRecord solution, WixProjectRecord wixProject) 
        => _solutionInstallers.Add(new SolutionWixProjectReference(solution.Name, wixProject.Name));

    public void AddWixProjectReference(WixProjectRecord wixProject, ProjectRecord project) 
        => _wixProjectReferences.Add(new WixProjectReference(wixProject.Name, project.Name));

    public void AddWixAssemblyReference(WixProjectRecord wixProject, ProjectRecord project) 
        => _wixAssemblyReferences.Add(new WixAssemblyReference(wixProject.Name, project.Name));

    public int ProjectFilesToScanCount => _projectFilesToScan.Count;

    public int WixProjectFilesToScanCount => _wixProjectFilesToScan.Count;

    public IEnumerable<ProjectRecord> GetProjectFilesToScan()
    {
        // custom code to enable a foreach loop on an active queue
        var enumerator = new QueueEnumerator<ProjectRecord>(_projectFilesToScan);
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    public IEnumerable<WixProjectRecord> GetWixProjectFilesToScan()
    {
        // custom code to enable a foreach loop on an active queue
        var enumerator = new QueueEnumerator<WixProjectRecord>(_wixProjectFilesToScan);
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }


    public ProjectRecord[] GetProjectsAvailableForWix(WixProjectRecord wixProject)
    {
        if (wixProject.Solutions.Count != 1)
            throw new InvalidOperationException($"Wix project ({wixProject.Path}) is referenced by {wixProject.Solutions.Count} project(s)");

        // get all projects that are required for this wix project (hint: use the solution as the root)
        var solutionName = wixProject.Solutions[0];

        if (!_solutionsByName.TryGetValue(solutionName, out var solution))
            throw new KeyNotFoundException($"Solution name not found {solutionName}");

        var projects = solution.Projects
            .Select(p => _projectsByName[p.Name])
            .Where(p =>
            {
                // file must exist!
                if (!p.DoesExist) 
                    return false;

                // ignore test projects
                return !_testProjectFilePathRegex.IsMatch(p.Path);

            })
            .ToArray();

        return projects;
    }

    public BranchDatabase CreateDatabase() => new()
    {
        Errors = _errors.ToList(),
        Solutions = _solutionsByPath.Values.ToList(),
        Projects = _projectsByPath.Values.ToList(),
        WixProjects = _wixProjectsByPath.Values.ToList()
    };

    #region Private Classes

    private class QueueEnumerator<T>(Queue<T> queue) : IEnumerator<T>
    {
        public T Current { get; private set; }

        public bool MoveNext()
        {
            if (queue.Count == 0)
                return false;

            Current = queue.Dequeue();
            return Current != null;
        }

        public void Reset() => Current = default!;

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            // no op
        }
    }

    #endregion

}
