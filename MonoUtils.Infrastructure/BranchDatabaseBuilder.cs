using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilderFactory(IServiceProvider serviceProvider)
{
    public BranchDatabaseBuilder Create(BuildDefinition[] builds)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        var resolver = serviceProvider.GetRequiredService<UniqueNameResolver>();
        return new BranchDatabaseBuilder(loggerFactory, fileStorage, resolver, builds);
    }
}

public class BranchDatabaseBuilder(ILoggerFactory loggerFactory, IFileStorage fileStorage, UniqueNameResolver resolver, BuildDefinition[] builds)
{
    private readonly ILogger<BranchDatabaseBuilder> logger = loggerFactory.CreateLogger<BranchDatabaseBuilder>();
    private readonly Dictionary<string, BuildDefinition> _requiredSolutions = builds.ToDictionary(s => s.SolutionPath);

    // only record names
    private readonly HashSet<string> _solutionNames = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> _projectNames = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> _wixProjNames = new(StringComparer.InvariantCultureIgnoreCase);

    // file path & record
    private readonly Dictionary<string, SolutionRecord> _solutions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, ProjectRecord> _projects = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, WixProjectRecord> _wixProjects = new(StringComparer.InvariantCultureIgnoreCase);

    // files to scan
    private readonly Queue<ProjectRecord> _projectFilesToScan = new();
    private readonly Queue<WixProjectRecord> _wixProjFilesToScan = new();

    private readonly List<string> _errors = new();

    public SolutionRecord GetOrAddSolution(string solutionPath)
    {
        // if the solution is referenced in one our build definitions, then it is considered a REQUIRED solution. 
        var isRequired = _requiredSolutions.TryGetValue(solutionPath, out var build);

        if (!_solutions.TryGetValue(solutionPath, out var solution))
        {
            var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            solutionName = resolver.GetUniqueName(solutionName, _solutionNames.Contains);
            var exists = fileStorage.FileExists(solutionPath);

            solution = new SolutionRecord(solutionName, solutionPath, isRequired, exists);
            if (build is not null)
                solution.Builds.Add(build.BuildName);

            _solutionNames.Add(solutionName);
            _solutions.Add(solutionPath, solution);
        }
        else
        {
            // as we build the solutions list, we may find that the solution is referenced
            // by another build definition. If so, we need to add the build name to the solution
            if (build is not null && !solution.Builds.Contains(build.BuildName))
            {
                if (!solution.IsRequired)
                    solution = solution with { IsRequired = true };

                solution.Builds.Add(build.BuildName);
            }
        }

        return solution;
    }

    public ProjectRecord GetOrAddProject(string projectPath, bool isRequired)
    {
        if (!_projects.TryGetValue(projectPath, out var project))
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            projectName = resolver.GetUniqueName(projectName, _projectNames.Contains);
            var exists = fileStorage.FileExists(projectPath);

            project = new ProjectRecord(projectName, projectPath, isRequired, exists);

            _projectNames.Add(projectName);
            _projects.Add(projectPath, project);

            // if the file exists, then we must queue the file to be scanned
            if (exists)
                _projectFilesToScan.Enqueue(project);
        }
        else
        {
            // update the "IsRequired" flag if necessary
            if (isRequired && !project.IsRequired)
                project = project with {IsRequired = true};
        }

        return project;
    }

    public WixProjectRecord GetOrAddWixProject(string projectPath, bool isRequired)
    {
        if (!_wixProjects.TryGetValue(projectPath, out var project))
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            projectName = resolver.GetUniqueName(projectName, _wixProjNames.Contains);
            var exists = fileStorage.FileExists(projectPath);

            project = new WixProjectRecord(projectName, projectPath, isRequired, exists);

            _wixProjNames.Add(projectName);
            _wixProjects.Add(projectPath, project);

            // if the file exists, then we must queue the file to be scanned once all projects are scanned
            // this is required to determine if the assembly name is referenced in any wxs files (final step)
            if (exists) 
                _wixProjFilesToScan.Enqueue(project);
        }
        else
        {
            // update the "IsRequired" flag if necessary
            if (isRequired && !project.IsRequired)
                project = project with { IsRequired = true };
        }

        return project;
    }

    public IEnumerable<ProjectRecord> GetProjectFilesToScan()
    {
        // custom code to enable a foreach loop on an active queue
        var enumerator = new ProjectFilesToScanEnumerator(_projectFilesToScan);
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    public IEnumerable<WixProjectRecord> GetWixProjectFilesToScan()
    {
        // custom code to enable a foreach loop on an active queue
        var enumerator = new WixProjectFilesToScanEnumerator(_wixProjFilesToScan);
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }

    public void AddError(string error) => _errors.Add(error);

    public BranchDatabase CreateDatabase() => new()
    {
        Errors = _errors.ToList(),
        Solutions = _solutions.Values.ToList(),
        Projects = _projects.Values.ToList(),
        WixProjects = _wixProjects.Values.ToList()
    };

    #region Private Classes

    private class ProjectFilesToScanEnumerator(Queue<ProjectRecord> queue) : IEnumerator<ProjectRecord>
    {
        public ProjectRecord Current { get; private set; }

        public bool MoveNext()
        {
            Current = queue.Dequeue();
            return queue.Count > 0;
        }

        public void Reset() => Current = null!;

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            // no op
        }
    }

    private class WixProjectFilesToScanEnumerator(Queue<WixProjectRecord> queue) : IEnumerator<WixProjectRecord>
    {
        public WixProjectRecord Current { get; private set; }

        public bool MoveNext()
        {
            Current = queue.Dequeue();
            return queue.Count > 0;
        }

        public void Reset() => Current = null!;

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            // no op
        }
    }

    #endregion
}
