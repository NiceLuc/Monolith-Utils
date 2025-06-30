using MonoUtils.Domain.Data.Queries;

namespace MonoUtils.Domain.Data;

public class BranchDatabaseContext
{
    private readonly Dictionary<string, ProjectRecord> _projects;

    private readonly Dictionary<string, SolutionRecord> _solutions;

    private readonly Dictionary<string, WixProjectRecord> _wixProjects;

    private readonly Dictionary<string, string[]> _buildDefinitions;

    public BranchDatabaseContext(BranchDatabase database)
    {
        _projects = database.Projects.ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);
        _solutions = database.Solutions.ToDictionary(s => s.Name, StringComparer.InvariantCultureIgnoreCase);
        _wixProjects = database.WixProjects.ToDictionary(w => w.Name, StringComparer.InvariantCultureIgnoreCase);
        _buildDefinitions = database.Solutions.ToDictionary(s => s.Name, s => s.Builds.ToArray(), StringComparer.InvariantCultureIgnoreCase);
    }

    public ProjectRecord[] GetProjectsReferencing(ProjectRecord project, ItemQuery<ProjectRecord> query, bool isRecursive = false)
    {
        ProjectRecord[] results;

        if (!isRecursive)
        {
            results = project.References.Select(p => _projects[p])
                .Where(query.IsActive).ToArray();
        }
        else
        {
            var records = new Dictionary<string, ProjectRecord>();
            CaptureProjectNames(records, project);
            results = records.Values.ToArray();
        }

        return SortedResults(results, query);

        void CaptureProjectNames(Dictionary<string, ProjectRecord> added, ProjectRecord current)
        {
            foreach (var name in current.References)
            {
                if (added.ContainsKey(name))
                    continue;

                var next = _projects[name];
                if (!query.IsActive(next))
                    continue;

                added.Add(name, next);

                CaptureProjectNames(added, next); // note: recursion!
            }
        }
    }

    public ProjectRecord[] GetProjectsReferencedBy(ProjectRecord project, ItemQuery<ProjectRecord> query)
    {
        ProjectRecord[] results;

        if (!query.IsRecursive)
        {
            results = project.ReferencedBy.Select(p => _projects[p]).ToArray();
        }
        else
        {
            var result = new Dictionary<string, ProjectRecord>();
            CaptureProjectNames(result, project);
            results = result.Values.ToArray();
        }

        return SortedResults(results, query);

        void CaptureProjectNames(Dictionary<string, ProjectRecord> result, ProjectRecord current)
        {
            foreach (var name in current.ReferencedBy)
            {
                if (result.ContainsKey(name))
                    continue;

                var next = _projects[name];
                result.Add(name, next);

                CaptureProjectNames(result, next); // note: recursion!
            }
        }
    }

    public string[] GetBuildDefinitionNames(ProjectRecord project)
    {
        var buildNames = project.Solutions.SelectMany(s => _buildDefinitions[s]);
        return buildNames.Distinct().OrderBy(b => b).ToArray();
    }

    public WixProjectRecord[] GetWixProjects(ProjectRecord project)
    {
        if (project.WixProjects.Length == 0)
            return [];

        return project.WixProjects.Select(w => _wixProjects[w.ProjectName]).ToArray();
    }

    public ProjectRecord? GetProject(string projectName)
    {
        return _projects.GetValueOrDefault(projectName);
    }

    public SolutionRecord? GetSolution(string solutionName)
    {
        return _solutions.GetValueOrDefault(solutionName);
    }

    private ProjectRecord[] SortedResults(ProjectRecord[] results, ItemQuery<ProjectRecord> query)
    {
        // todo: support different sort options
        var sortedResults = query.IsDescending
            ? results.OrderByDescending(p => p.Name)
            : results.OrderBy(p => p.Name);

        return sortedResults.ToArray();
    }
}