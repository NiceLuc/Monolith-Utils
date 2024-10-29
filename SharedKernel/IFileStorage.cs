namespace SharedKernel;

public interface IFileStorage
{
    Stream GetFileStreamForRead(string filePath);
    Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken);
    Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken);
}