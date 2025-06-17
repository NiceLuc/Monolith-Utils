using System.Text.RegularExpressions;
using MonoUtils.Domain;

namespace MonoUtils.Infrastructure.FileScanners;

public class StandardProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _csProjSdkRegex = new(@"<Project Sdk=", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjNetStandardRegex = new(@"\<TargetFrameworks?\>.*netstandard2\.\d.*\<\/TargetFrameworks?\>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _testProjectFilePathRegex = new(@"Tests?\.csproj", RegexOptions.Multiline);

    public async Task<Results> ScanAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileStorage.FileExists(path))
            throw new FileNotFoundException($"Project file not found: {path}");

        var projectXml = await fileStorage.ReadAllTextAsync(path, cancellationToken); // todo: ignore comments!!
        var assemblyName = GetAssemblyName(path, projectXml);
        var projectDirectory = Path.GetDirectoryName(path)!;
        var packagesConfig = Path.Combine(projectDirectory, "packages.config");

        var results = new Results
        {
            AssemblyName = assemblyName,
            PdbFileName = GetPdbFileName(assemblyName),
            IsSdk = _csProjSdkRegex.IsMatch(projectXml),
            IsNetStandard2 = _csProjNetStandardRegex.IsMatch(projectXml),
            IsPackageRef = !fileStorage.FileExists(packagesConfig),
            IsTestProject = _testProjectFilePathRegex.IsMatch(path)
        };

        foreach (Match match in _csProjReferenceRegex.Matches(projectXml))
        {
            var relativePath = Path.Combine(projectDirectory, match.Groups["project_path"].Value);
            var referencePath = Path.GetFullPath(relativePath);
            results.References.Add(referencePath);
        }

        return results;
    }

    private static string GetAssemblyName(string projectFilePath, string projectXml)
    {
        // Define regex patterns
        const string assemblyNamePattern = @"<AssemblyName>(.*?)<\/AssemblyName>";
        const string outputTypePattern = @"<OutputType>(.*?)<\/OutputType>";

        // Match AssemblyName
        var assemblyNameMatch = Regex.Match(projectXml, assemblyNamePattern, RegexOptions.IgnoreCase);
        var assemblyName = assemblyNameMatch.Success
            ? assemblyNameMatch.Groups[1].Value
            : Path.GetFileNameWithoutExtension(projectFilePath);

        // Match OutputType
        var outputTypeMatch = Regex.Match(projectXml, outputTypePattern, RegexOptions.IgnoreCase);
        var outputType = outputTypeMatch.Success ? outputTypeMatch.Groups[1].Value : "Library";

        // Determine the file extension
        var extension = outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) ? ".exe" : ".dll";

        // Return the assembly name with the correct extension
        return $"{assemblyName}{extension}";
    }

    private static string GetPdbFileName(string assemblyName)
    {
        var lastIndex = assemblyName.LastIndexOf('.');
        if (lastIndex < 0)
            return assemblyName + ".pdb";

        return assemblyName[..lastIndex] + ".pdb";
    }

    public class Results
    {
        public string AssemblyName { get; set; }
        public string PdbFileName { get; set; }
        public bool IsSdk { get; set; }
        public bool IsNetStandard2 { get; set; }
        public bool IsPackageRef { get; set; }
        public bool IsTestProject { get; set; }
        public List<string> References { get; } = new();
    }
}