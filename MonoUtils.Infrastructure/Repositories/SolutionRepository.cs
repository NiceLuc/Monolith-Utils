using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure.Repositories;

public class SolutionRepository(IFileStorage fileStorage, UniqueNameResolver resolver) : IRecordRepository<SolutionRecord>
{
    private readonly Dictionary<string, SolutionRecord> _solutions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> _solutionNames = new(StringComparer.InvariantCultureIgnoreCase);

    public bool TryGetRecord(string filePath, out SolutionRecord record) 
        => _solutions.TryGetValue(filePath, out record!);

    public SolutionRecord AddRecord(string filePath, bool isRequired)
    {
        var solutionName = Path.GetFileNameWithoutExtension(filePath);
        solutionName = resolver.GetUniqueName(solutionName, _solutionNames.Contains);
        var exists = fileStorage.FileExists(filePath);

        var solution = new SolutionRecord(solutionName, filePath, isRequired, exists);
        _solutions.Add(filePath, solution);
        _solutionNames.Add(solutionName);

        return solution;
    }

    public SolutionRecord[] GetRecords() => _solutions.Values.ToArray();
}