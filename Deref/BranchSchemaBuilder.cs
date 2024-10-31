namespace Deref;

public class BranchSchemaBuilder()
{
    private readonly Dictionary<string, string> _projectPathToName = new();
    private readonly Dictionary<string, string> _projectNameToPath = new();
    private readonly Dictionary<string, List<string>> _projectReferences = new();

    private readonly Dictionary<string, string> _solutionPathToName = new();
    private readonly Dictionary<string, List<string>> _solutionProjects = new();

    public ProjectToken AddProject(string path)
    {
        if (_projectPathToName.TryGetValue(path, out var name))
            return new ProjectToken(name, path);

        var baseName = Path.GetFileNameWithoutExtension(path);
        name = GetUniqueName(baseName, _projectNameToPath.ContainsKey);

        _projectPathToName.Add(path, name);
        _projectNameToPath.Add(name, path);
        _projectReferences.Add(name, []);
        return new ProjectToken(name, path);
    }

    public SolutionToken AddSolution(string path)
    {
        if (_solutionPathToName.TryGetValue(path, out var name))
            return new SolutionToken(name, path);

        var baseName = Path.GetFileNameWithoutExtension(path);
        name = GetUniqueName(baseName, _solutionProjects.ContainsKey);

        _solutionPathToName.Add(path, name);
        _solutionProjects.Add(name, []);
        return new SolutionToken(name, path);
    }

    public void AssignProjectToSolution(SolutionToken solution, ProjectToken project)
    {
        if (!_solutionProjects.TryGetValue(solution.Name, out var projects))
            throw new InvalidOperationException($"List not configured for solution: {solution.Name}");

        if (projects.Contains(project.Name))
            throw new InvalidOperationException(
                $"Solution {solution.Name} already references this project: {project.Name}");

        projects.Add(project.Name);
    }

    public void AddReference(ProjectToken project, string referencedProjectPath)
    {
        if (!_projectPathToName.TryGetValue(referencedProjectPath, out var referenceName))
            throw new InvalidOperationException($"Project not found: {referencedProjectPath}");

        if (!_projectReferences.TryGetValue(project.Name, out var projects))
            throw new InvalidOperationException($"Project list not configured for: {project.Name}");

        if (projects.Contains(referenceName))
            throw new InvalidOperationException($"Project {project.Name} already references {referenceName}");

        projects.Add(referenceName);
    }

    public BranchSchema Build()
    {
        var projects = _projectNameToPath.Select(pair
            => new BranchSchema.Project
            {
                Name = pair.Key,
                Path = pair.Value
            });

        var solutions = _solutionPathToName.Select(pair
            => new BranchSchema.Solution
            {
                Name = pair.Value,
                Path = pair.Key,
                Projects = _solutionProjects[pair.Value]
            });

        /* todo: add references
        var references = _projectReferences.Select(pair
            => new BranchSchema.ProjectReference
            {
                Name = pair.Key,
                UsedBy = _projectReferences.Where(p => p.Value.Contains(pair.Key)).Select(p => p.Key).ToList(),
                Uses = pair.Value
            });
        */

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

    public record ProjectToken(string Name, string Path);
    public record SolutionToken(string Name, string Path);
}