﻿using MonoUtils.Domain;

namespace MonoUtils.Infrastructure;

public class FileStorage : IFileStorage
{
    public async Task WriteAllTextAsync(string filePath, string content, CancellationToken cancellationToken) 
        => await File.WriteAllTextAsync(filePath, content, cancellationToken);

    public async Task<string> ReadAllTextAsync(string filePath, CancellationToken cancellationToken)
        => await File.ReadAllTextAsync(filePath, cancellationToken);

    public Stream GetFileStreamForRead(string filePath) => File.OpenRead(filePath);

    public StreamReader GetStreamReader(string filePath) => File.OpenText(filePath);

    public bool FileExists(string filePath) => File.Exists(filePath);

    public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

    public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

    public string[] GetFilePaths(string rootDirectory, string pattern, SearchOption options = SearchOption.AllDirectories) 
        => Directory.EnumerateFiles(rootDirectory, pattern, options).ToArray();

    public string[] GetDirectoryNames(string rootDirectory) =>
        Directory.EnumerateDirectories(rootDirectory).Select(d => new DirectoryInfo(d).Name).ToArray();
}