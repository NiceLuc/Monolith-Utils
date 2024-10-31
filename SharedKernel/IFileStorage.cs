namespace SharedKernel;

public interface IFileStorage
{
    Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken);
    Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken);

    Stream GetFileStreamForRead(string filePath);
    StreamReader GetStreamReader(string filePath);

    bool FileExists(string filePath);
    bool DirectoryExists(string directoryPath);
    void CreateDirectory(string directoryPath);

    string[] GetFilePaths(string rootDirectory, string pattern, SearchOption options = SearchOption.AllDirectories);
}