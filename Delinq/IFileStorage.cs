namespace Delinq;

public interface IFileStorage
{
    Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken);
    Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken);
}