using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixComponentFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _assemblyNameRegex = new(@"File.+?Source=""\$\(.+?\)(?<assembly_name>.+?\.dll)""", RegexOptions.Multiline);

    public async Task<WixComponentScanResults> ScanAsync(BranchDatabaseBuilder builder, WixProjectRecord wixProject, string filePath, CancellationToken cancellationToken)
    {
        // get all projects that are required for this wix project (hint: use the solution as the root)
        var projects = builder.GetProjectsAvailableForWix(wixProject);
        var assemblyNames = projects.ToDictionary(p => p.AssemblyName);

        var results = new WixComponentScanResults();

        // find all harvested projects associated with this component file
        var wxsXml = await fileStorage.ReadAllTextAsync(filePath, cancellationToken);
        foreach (Match match in _assemblyNameRegex.Matches(wxsXml))
        {
            var assemblyName = match.Groups["assembly_name"].Value;
            var assemblyNameIndex = assemblyName.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
            if (assemblyNameIndex >= 0)
                assemblyName = assemblyName[(assemblyNameIndex + 1)..];

            // this is not a project reference, but some other random harvested binary
            if (!assemblyNames.TryGetValue(assemblyName, out var project))
                continue;

            // csharp project is required for this wix project (harvested)
            results.ProjectPaths.Add(project.Path);
        }

        return results;
    }

    public class WixComponentScanResults
    {
        public List<string> ProjectPaths { get; } = new();
    }
}