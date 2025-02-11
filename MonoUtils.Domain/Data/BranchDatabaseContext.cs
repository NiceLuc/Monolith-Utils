using MonoUtils.Domain.Data.Queries;

namespace MonoUtils.Domain.Data;

public class BranchDatabaseContext(BranchDatabase database)
{
    private readonly Lazy<Dictionary<string, ProjectRecord>> _projects = new(() =>
        database.Projects.ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase));

    private readonly Lazy<Dictionary<string, SolutionRecord>> _solutions = new(() =>
        database.Solutions.ToDictionary(s => s.Name, StringComparer.InvariantCultureIgnoreCase));

    private readonly Lazy<Dictionary<string, WixProjectRecord>> _wixProjects = new(() =>
        database.WixProjects.ToDictionary(w => w.Name, StringComparer.InvariantCultureIgnoreCase));

    private readonly Lazy<Dictionary<string, string[]>> _buildDefinitions = new(() => 
        database.Solutions.ToDictionary(s => s.Name, s => s.Builds.ToArray(), StringComparer.InvariantCultureIgnoreCase));

    public ProjectRecord[] GetProjectsReferencing(ProjectRecord project, ItemQuery<ProjectRecord> query, bool isRecursive = false)
    {
        var lookup = _projects.Value;
        ProjectRecord[] results;

        if (!isRecursive)
        {
            results = project.References.Select(p => lookup[p])
                .Where(query.IsActive).ToArray();

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
            foreach (var name in current.References)
            {
                if (result.ContainsKey(name))
                    continue;

                var next = lookup[name];
                if (!query.IsActive(next))
                    continue;

                result.Add(name, next);

                CaptureProjectNames(result, next); // note: recursion!
            }
        }
    }

    public ProjectRecord[] GetProjectsReferencedBy(ProjectRecord project, ItemQuery<ProjectRecord> query)
    {
        var lookup = _projects.Value;
        ProjectRecord[] results;

        if (!query.IsRecursive)
        {
            results = project.ReferencedBy.Select(p => lookup[p]).ToArray();
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

                var next = lookup[name];
                result.Add(name, next);

                CaptureProjectNames(result, next); // note: recursion!
            }
        }
    }

    public string[] GetBuildDefinitionNames(ProjectRecord project)
    {
        var lookup = _buildDefinitions.Value;
        var buildNames = project.Solutions.SelectMany(s => lookup[s]);
        return buildNames.Distinct().OrderBy(b => b).ToArray();
    }

    public WixProjectRecord[] GetWixProjects(ProjectRecord project)
    {
        if (project.WixProjects.Count == 0)
            return [];

        var lookup = _wixProjects.Value;
        return project.WixProjects.Select(w => lookup[w.ProjectName]).ToArray();
    }

    public ProjectRecord? GetProject(string projectName)
    {
        var lookup = _projects.Value;
        return lookup.GetValueOrDefault(projectName);
    }

    public SolutionRecord? GetSolution(string solutionName)
    {
        var lookup = _solutions.Value;
        return lookup.GetValueOrDefault(solutionName);
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