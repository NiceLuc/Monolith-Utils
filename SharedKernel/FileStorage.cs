﻿namespace SharedKernel;

public class FileStorage : IFileStorage
{
    public Stream GetFileStreamForRead(string filePath) 
        => File.OpenRead(filePath);

    public Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken) 
        => File.WriteAllTextAsync(filePath, content, cancellationToken);

    public Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken)
        => File.ReadAllTextAsync(filePath, cancellationToken);
}