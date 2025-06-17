using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure;

public class RecordProvider<T>(UniqueNameResolver resolver, IFileStorage fileStorage) where T : SchemaRecord
{
    internal readonly Dictionary<string, T> _byName = new(StringComparer.InvariantCultureIgnoreCase);
    internal readonly Dictionary<string, T> _byPath = new(StringComparer.InvariantCultureIgnoreCase);

    public T GetOrAdd(string path, Func<string, bool, T>? factory = null)
    {
        if (_byPath.TryGetValue(path, out var record))
            return record;

        if (factory is null)
            throw new ArgumentNullException(nameof(factory), $"Factory function must be provided to create a new {typeof(T).Name}.");

        var name = Path.GetFileNameWithoutExtension(path);
        name = resolver.GetUniqueName(name, _byName.ContainsKey);
        var exists = fileStorage.FileExists(path);

        record = factory(name, exists);

        _byName.Add(name, record);
        _byPath.Add(path, record);

        return record;
    }

    public void UpdateRecord(T record)
    {
        _byPath[record.Path] = record;
        _byName[record.Name] = record;
    }

    public IList<T> GetRecords() => _byPath.Values.ToList();

    public T GetRecordByPath(string path) => _byPath[path];

    public T GetRecordByName(string name) => _byName[name];
}