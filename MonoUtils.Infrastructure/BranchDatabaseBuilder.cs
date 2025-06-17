using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilder(
    RecordProvider<SolutionRecord> solutionProvider,
    RecordProvider<ProjectRecord> projectProvider, 
    RecordProvider<WixProjectRecord> wixProjectProvider)
{
    // required solutions (i.e. build definitions have a reference to a solution path)
    private readonly List<ErrorRecord> _errors = new();


    public void AddError(SolutionRecord solution, string message, ErrorSeverity severity)
    {
        _errors.Add(new ErrorRecord(RecordType.Solution, solution.Name, message, severity));
    }

    public void AddError(ProjectRecord project, string message, ErrorSeverity severity)
    {
        _errors.Add(new ErrorRecord(RecordType.Project, project.Name, message, severity));
    }

    public void AddError(WixProjectRecord wixProject, string message, ErrorSeverity severity)
    {
        _errors.Add(new ErrorRecord(RecordType.WixProject, wixProject.Name, message, severity));
    }


    public SolutionRecord GetOrAddSolution(string solutionPath) 
        => solutionProvider.GetOrAdd(solutionPath, (name, exists) 
            => new SolutionRecord(name, solutionPath, exists));

    public ProjectRecord GetOrAddProject(string projectPath)
        => projectProvider.GetOrAdd(projectPath, (name, exists) 
            => new ProjectRecord(name, projectPath, exists));

    public WixProjectRecord GetOrAddWixProject(string projectPath) 
        => wixProjectProvider.GetOrAdd(projectPath, (name, exists)
            => new WixProjectRecord(name, projectPath, exists));


    public void UpdateProject(ProjectRecord project)
    {
        projectProvider.UpdateRecord(project);
    }

    public void UpdateWixProject(WixProjectRecord wixProject)
    {
        wixProjectProvider.UpdateRecord(wixProject);
    }

    public ProjectRecord[] GetProjectsAvailableForInstallers(SolutionRecord solution)
    {
        var projects = GetProjectsForSolutions(solution);
        return projects.Where(p => p is {DoesExist: true, IsTestProject: false}).ToArray();
    }

    public BranchDatabase CreateDatabase()
    {
        // any solution that has a build definition
        // is considered REQUIRED!
        var solutions = (from s in solutionProvider.GetRecords()
            where s.Builds.Any()
            select s).ToArray();

        SetRequiredSolutions(solutions);
        SetRequiredProjects(solutions);
        SetRequiredWixProjects(solutions);

        return new BranchDatabase
        {
            Errors = _errors.ToList(),
            Solutions = solutionProvider.GetRecords(),
            Projects = projectProvider.GetRecords(),
            WixProjects = wixProjectProvider.GetRecords()
        };
    }


    private void SetRequiredSolutions(SolutionRecord[] solutions)
    {
        foreach (var solution in solutions)
            solutionProvider.UpdateRecord(solution with {IsRequired = true});
    }

    private void SetRequiredProjects(SolutionRecord[] solutions)
    {
        // we immediately have required projects from all required solutions
        var projects = GetProjectsForSolutions(solutions);

        // now update all records to indicate they are required
        foreach (var project in projects)
            projectProvider.UpdateRecord(project with {IsRequired = true});
    }

    private void SetRequiredWixProjects(SolutionRecord[] solutions)
    {
        var result =
            (from s in solutions
                from p in s.WixProjects
                select p)
            .Distinct()
            .Select(wixProjectProvider.GetRecordByName)
            .ToArray();

        foreach (var wixProject in result)
            wixProjectProvider.UpdateRecord(wixProject with { IsRequired = true });
    }

    private ProjectRecord[] GetProjectsForSolutions(params SolutionRecord[] solutions)
    {
        // we immediately have required projects from all required solutions
        var result =
            (from s in solutions
                from p in s.Projects
                select p.ProjectName)
            .Distinct()
            .ToDictionary(n => n, projectProvider.GetRecordByName);

        // get all references recursively for these projects
        var projects = result.Select(kv => kv.Value).ToArray();
        var lookup = projectProvider._byName;
        foreach (var project in projects)
            CollectAllReferencesFrom(project);

        return result.Values.ToArray();

        // helper method!
        void CollectAllReferencesFrom(ProjectRecord current)
        {
            foreach (var name in current.References)
            {
                if (result.ContainsKey(name))
                    continue;

                var next = lookup[name];
                result.Add(name, next);

                CollectAllReferencesFrom(next); // note: recursion!
            }
        }

        /*
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

         */
        return [];
    }

}
