namespace SharedKernel;

public class FileStorage : IFileStorage
{
    public Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken) 
        => File.WriteAllTextAsync(filePath, content, cancellationToken);

    public Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken)
        => File.ReadAllTextAsync(filePath, cancellationToken);

    public Stream GetFileStreamForRead(string filePath) => File.OpenRead(filePath);

    public StreamReader GetStreamReader(string filePath) => File.OpenText(filePath);

    public bool FileExists(string filePath) => File.Exists(filePath);

    public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

    public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);
}