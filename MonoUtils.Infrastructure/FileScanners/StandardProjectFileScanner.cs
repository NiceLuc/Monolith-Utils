using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class StandardProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _csProjSdkRegex = new(@"<Project Sdk=", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjNetStandardRegex = new(@"\<TargetFrameworks?\>.*netstandard2\.\d.*\<\/TargetFrameworks?\>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public async Task<StandardProjectScanResults> ScanAsync(ProjectRecord project, CancellationToken cancellationToken)
    {
        var result = new StandardProjectScanResults(project);

        // cannot scan a file that does not exist
        if (!project.DoesExist)
            return result;

        var projectXml = await fileStorage.ReadAllTextAsync(project.Path, cancellationToken); // todo: ignore comments!!
        project.AssemblyName = GetAssemblyName(project.Path, projectXml);
        project.PdbFileName = GetPdbFileName(project.AssemblyName);
        project.IsSdk = _csProjSdkRegex.IsMatch(projectXml);
        project.IsNetStandard2 = _csProjNetStandardRegex.IsMatch(projectXml);

        var projectDirectory = Path.GetDirectoryName(project.Path)!;
        var packagesConfig = Path.Combine(projectDirectory, "packages.config");
        project.IsPackageRef = !fileStorage.FileExists(packagesConfig);

        foreach (Match match in _csProjReferenceRegex.Matches(projectXml))
        {
            var relativePath = Path.Combine(projectDirectory, match.Groups["project_path"].Value);
            var referencePath = Path.GetFullPath(relativePath);
            result.References.Add(referencePath);
        }

        return result;
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

    public class StandardProjectScanResults(ProjectRecord project)
    {
        public ProjectRecord Project { get; } = project;
        public List<string> References { get; } = new();
    }
}