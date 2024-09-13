namespace Delinq;

public class FileStorage : IFileStorage
{
    public Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken) 
        => File.WriteAllTextAsync(filePath, content, cancellationToken);
}