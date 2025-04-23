using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure.FileScanners;

public class StandardProjectFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _csProjReferenceRegex = new(@"ProjectReference Include=""(?<project_path>.+?\.(cs|db|sql)proj)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjSdkRegex = new(@"<Project Sdk=", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex _csProjNetStandardRegex = new(@"\<TargetFrameworks?\>.*netstandard2\.\d.*\<\/TargetFrameworks?\>", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public async Task<Results> ScanAsync(BranchDatabaseBuilder builder, ProjectRecord project, CancellationToken cancellationToken)
    {
        var projectXml = await fileStorage.ReadAllTextAsync(project.Path, cancellationToken); // todo: ignore comments!!
        project.AssemblyName = GetAssemblyName(project.Path, projectXml);
        project.PdbFileName = GetPdbFileName(project.AssemblyName);
        project.IsSdk = _csProjSdkRegex.IsMatch(projectXml);
        project.IsNetStandard2 = _csProjNetStandardRegex.IsMatch(projectXml);

        var projectDirectory = Path.GetDirectoryName(project.Path)!;
        var packagesConfig = Path.Combine(projectDirectory, "packages.config");
        project.IsPackageRef = !fileStorage.FileExists(packagesConfig);

        var result = new Results(project);
        foreach (Match match in _csProjReferenceRegex.Matches(projectXml))
        {
            var relativePath = Path.Combine(projectDirectory, match.Groups["project_path"].Value);
            var referencePath = Path.GetFullPath(relativePath);

            var reference = builder.GetOrAddProject(referencePath, project.IsRequired);
            project.References.Add(reference.Name);
            reference.ReferencedBy.Add(project.Name);
            result.References.Add(reference);
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

    public class Results(ProjectRecord project)
    {
        public ProjectRecord Project { get; } = project;
        public List<ProjectRecord> References { get; } = new();
    }
}