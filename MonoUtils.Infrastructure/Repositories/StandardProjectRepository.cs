using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure.Repositories;

public class StandardProjectRepository(IFileStorage fileStorage, UniqueNameResolver resolver) : IRecordRepository<ProjectRecord>
{
    private readonly Dictionary<string, ProjectRecord> _projects = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> _projectNames = new(StringComparer.InvariantCultureIgnoreCase);

    public bool TryGetRecord(string filePath, out ProjectRecord? record)
        => _projects.TryGetValue(filePath, out record);

    public ProjectRecord AddRecord(string filePath, bool isRequired)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);
        projectName = resolver.GetUniqueName(projectName, _projectNames.Contains);
        var exists = fileStorage.FileExists(filePath);

        var project = new ProjectRecord(projectName, filePath, exists);
        _projects.Add(filePath, project);
        _projectNames.Add(projectName);

        return project;
    }

    public ProjectRecord[] GetRecords() => _projects.Values.ToArray();
}