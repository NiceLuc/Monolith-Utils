using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilder(
    RecordProvider<SolutionRecord> solutionProvider,
    RecordProvider<ProjectRecord> projectProvider, 
    RecordProvider<WixProjectRecord> wixProjectProvider) : IBranchDatabaseBuilder
{
    // required solutions (i.e. build definitions have a reference to a solution path)
    private readonly List<ErrorRecord> _errors = new();
    private readonly List<BuildDefinitionSolutionRef> _buildSolutions = new();
    private readonly List<SolutionProjectRef> _solutionProjects = new();
    private readonly List<SolutionWixProjectRef> _solutionWixProjects = new();
    private readonly List<ProjectRef> _projectReferences = new();
    private readonly List<WixProjectRef> _projectWixReferences = new();

    public void AddError<T>(T record, string message, Exception? error = null) where T: SchemaRecord
    {
        var type = record switch
        {
            SolutionRecord => RecordType.Solution,
            ProjectRecord => RecordType.Project,
            WixProjectRecord => RecordType.WixProject,
            _ => throw new ArgumentException("Unsupported record type", nameof(record))
        };

        message = error is not null 
            ? $"{message} (Exception: {error.Message})" 
            : message;

        _errors.Add(new ErrorRecord(type, record.Name, message, ErrorSeverity.Critical));
    }

    public void AddWarning<T>(T record, string message) where T : SchemaRecord
    {
        var type = record switch
        {
            SolutionRecord => RecordType.Solution,
            ProjectRecord => RecordType.Project,
            WixProjectRecord => RecordType.WixProject,
            _ => throw new ArgumentException("Unsupported record type", nameof(record))
        };

        _errors.Add(new ErrorRecord(type, record.Name, message, ErrorSeverity.Warning));
    }


    public void AddBuildSolution(SolutionRecord solution, string buildName)
    {
        // add a build definition to all solutions
        _buildSolutions.Add(new BuildDefinitionSolutionRef(buildName, solution.Name));
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

    public void AddProjectToSolution(SolutionRecord solution, ProjectRecord project, ProjectType itemType)
    {
        _solutionProjects.Add(new SolutionProjectRef(solution.Name, project.Name, itemType));
    }

    public void AddWixProjectToSolution(SolutionRecord solution, WixProjectRecord wixProject)
    {
        _solutionWixProjects.Add(new SolutionWixProjectRef(solution.Name, wixProject.Name));
    }

    public void AddProjectReference(ProjectRecord project, ProjectRecord reference)
    {
        _projectReferences.Add(new ProjectRef(project.Name, reference.Name));
    }

    public void AddWixProjectReference(WixProjectRecord wixProject, ProjectRecord reference, bool isManuallyHarvested)
    {
        _projectWixReferences.Add(new WixProjectRef(wixProject.Name, reference.Name, isManuallyHarvested));
    }


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
        AttachBuildNamesToSolutions();
        AttachProjectsToSolutions();
        AttachWixProjectsToSolutions();
        AttachProjectReferences();
        AttachProjectsToWixProjects();

        /*
        AttachErrorsToSolutions();
        AttachErrorsToProjects();
        AttachErrorsToWixProjects();
        */

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

    private void AttachProjectsToWixProjects()
    {
        // now attach all wix projects to the projects
        var wixProjectReferences = from w in _projectWixReferences
            group w by w.ProjectName into g
            select new
            {
                ProjectName = g.Key,
                WixProjects = g.Select(w => new WixProjectReference(w.WixProjectName, w.IsManuallyHarvested)).ToArray()
            };

        foreach (var item in wixProjectReferences)
        {
            var project = projectProvider.GetRecordByName(item.ProjectName);
            project.WixProjects = item.WixProjects;
        }

        // now attach all projects to the wix projects
        var projectWixReferences = from w in _projectWixReferences
            group w by w.WixProjectName into g
            select new
            {
                WixProjectName = g.Key,
                Projects = g.Select(p => new WixProjectReference(p.ProjectName, p.IsManuallyHarvested)).ToArray()
            };

        foreach (var item in projectWixReferences)
        {
            var wix = wixProjectProvider.GetRecordByName(item.WixProjectName);
            wix.ProjectReferences = item.Projects;
        }
    }

    private void AttachProjectReferences()
    {
        // add all project references to the projects
        var sourceProjects = from p in _projectReferences
            group p by p.ProjectName into g
            select new
            {
                ProjectName = g.Key,
                References = g.Select(r => r.ReferenceName).ToArray()
            };

        foreach (var item in sourceProjects)
        {
            var project = projectProvider.GetRecordByName(item.ProjectName);
            project.References = item.References;
        }

        // add all back referencing projects to the references
        var referencedProjects = from p in _projectReferences
            group p by p.ReferenceName into g
            select new
            {
                ReferenceName = g.Key,
                ReferencedBy = g.Select(r => r.ProjectName).ToArray()
            };

        foreach (var item in referencedProjects)
        {
            var project = projectProvider.GetRecordByName(item.ReferenceName);
            project.ReferencedBy = item.ReferencedBy;
        }
    }

    private void AttachWixProjectsToSolutions()
    {
        // add all wix projects to the solutions
        var solutionProjects = from w in _solutionWixProjects
            group w by w.SolutionName into g
            select new
            {
                SolutionName = g.Key,
                WixProjects = g.Select(w => w.WixProjectName).ToArray()
            };

        foreach (var pairs in solutionProjects)
        {
            var solution = solutionProvider.GetRecordByName(pairs.SolutionName);
            solution.WixProjects = pairs.WixProjects;
        }

        // now attach all solutions to all wix projects
        var projectSolutions = from w in _solutionWixProjects
            group w by w.WixProjectName into g
            select new
            {
                WixProjectName = g.Key,
                SolutionNames = g.Select(s => s.SolutionName).ToArray()
            };

        foreach (var pairs in projectSolutions)
        {
            var wix = wixProjectProvider.GetRecordByName(pairs.WixProjectName);
            wix.Solutions = pairs.SolutionNames;
        }
    }

    private void AttachProjectsToSolutions()
    {
        // attach all projects to all solutions
        var solutionProjects = (from p in _solutionProjects
            group p by p.SolutionName into g
            select new
            {
                SolutionName = g.Key,
                Projects = g.Select(i => new {i.ProjectName, i.Type}).ToArray()
            }).ToArray();

        foreach (var item in solutionProjects)
        {
            var solution = solutionProvider.GetRecordByName(item.SolutionName);
            solution.Projects = (from i in item.Projects
                select new SolutionProjectReference(i.ProjectName, i.Type)).ToArray();
        }

        // now attach all solutions to all projects
        var projectSolutions = (from p in _solutionProjects
            group p by p.ProjectName into g
            select new
            {
                ProjectName = g.Key,
                SolutionNames = g.Select(s => s.SolutionName).ToArray()
            }).ToArray();

        foreach (var item in projectSolutions)
        {
            var project = projectProvider.GetRecordByName(item.ProjectName);
            project.Solutions = item.SolutionNames;
        }
    }

    private void AttachBuildNamesToSolutions()
    {
        // add all build definitions to the solutions
        var solutionsAndBuilds = from b in _buildSolutions
            group b by b.SolutionName into g
            select new
            {
                SolutionName = g.Key,
                Builds = g.Select(b => b.BuildName).ToArray()
            };

        foreach (var pairs in solutionsAndBuilds)
        {
            var solution = solutionProvider.GetRecordByName(pairs.SolutionName);
            solution.Builds = pairs.Builds;
        }
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

    private record struct BuildDefinitionSolutionRef(string BuildName, string SolutionName);
    private record struct SolutionProjectRef(string SolutionName, string ProjectName, ProjectType Type);
    private record struct SolutionWixProjectRef(string SolutionName, string WixProjectName);
    private record struct ProjectRef(string ProjectName, string ReferenceName);
    private record struct WixProjectRef(string WixProjectName, string ProjectName, bool IsManuallyHarvested);
}
