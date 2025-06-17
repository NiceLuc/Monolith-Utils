using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure.Repositories;

public class WixProjectRepository(IFileStorage fileStorage, UniqueNameResolver resolver) : IRecordRepository<WixProjectRecord>
{
    private readonly Dictionary<string, WixProjectRecord> _wixProjects = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> _wixProjNames = new(StringComparer.InvariantCultureIgnoreCase);

    public bool TryGetRecord(string filePath, out WixProjectRecord? record)
        => _wixProjects.TryGetValue(filePath, out record);

    public WixProjectRecord AddRecord(string filePath, bool isRequired)
    {
        var projectName = Path.GetFileNameWithoutExtension(filePath);
        projectName = resolver.GetUniqueName(projectName, _wixProjNames.Contains);
        var exists = fileStorage.FileExists(filePath);

        var project = new WixProjectRecord(projectName, filePath, exists);
        _wixProjects.Add(filePath, project);
        _wixProjNames.Add(projectName);

        return project;
    }

    public WixProjectRecord[] GetRecords() => _wixProjects.Values.ToArray();
}