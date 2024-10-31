namespace Deref;

public class BranchSchemaBuilder(string rootPath)
{
    private readonly Dictionary<string, string> _projectPathToName = new();
    private readonly Dictionary<string, string> _projectNameToPath = new();
    private readonly Dictionary<string, List<string>> _projectReferences = new();

    private readonly Dictionary<string, string> _solutionPathToName = new();
    private readonly Dictionary<string, List<string>> _solutionProjects = new();

    public ProjectToken AddProject(string path)
    {
        if (!path.StartsWith(rootPath))
            path = Path.Combine(rootPath, path);

        if (_projectPathToName.TryGetValue(path, out var name))
            throw new InvalidOperationException($"Project already added: {path}");

        var baseName = Path.GetFileNameWithoutExtension(path);
        name = GetUniqueName(baseName, _projectNameToPath.ContainsKey);

        _projectPathToName.Add(path, name);
        _projectNameToPath.Add(name, path);
        _projectReferences.Add(name, []);
        return new ProjectToken(name, path);
    }

    public SolutionToken AddSolution(string path)
    {
        if (!path.StartsWith(rootPath))
            path = Path.Combine(rootPath, path);

        if (_solutionPathToName.TryGetValue(path, out var name))
            throw new InvalidOperationException($"Solution already added: {path}");

        var baseName = Path.GetFileNameWithoutExtension(path);
        name = GetUniqueName(baseName, _solutionProjects.ContainsKey);

        _solutionPathToName.Add(path, name);
        _solutionProjects.Add(name, []);
        return new SolutionToken(name, path);
    }

    public void AssignProjectToSolution(SolutionToken solution, string referencedProjectPath)
    {
        if (!_projectPathToName.TryGetValue(referencedProjectPath, out var projectName))
            throw new InvalidOperationException($"Solution {solution.Name} project not found: {referencedProjectPath}");

        if (!_solutionProjects.TryGetValue(solution.Name, out var projects))
            throw new InvalidOperationException($"List not configured for solution: {solution.Name}");

        if (projects.Contains(projectName))
            throw new InvalidOperationException(
                $"Solution {solution.Name} already references this project: {projectName}");

        projects.Add(projectName);
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
            => new BranchSchema.Project { Name = pair.Key, Path = pair.Value });

        var solutions = _solutionPathToName.Select(pair
            => new BranchSchema.Solution
            {
                Name = pair.Key,
                Path = pair.Value,
                ProjectKeys = _solutionProjects[pair.Key]
            });

        return new BranchSchema
        {

        };
    }

    private static string GetUniqueName(string baseName, Func<string, bool> hasKey)
    {
        var offset = 0;

        var name = baseName;
        while (!hasKey(name))
        {
            offset++;
            name = $"{baseName}-{offset}";
        }

        return name;
    }

    public record ProjectToken(string Name, string Path);
    public record SolutionToken(string Name, string Path);
}