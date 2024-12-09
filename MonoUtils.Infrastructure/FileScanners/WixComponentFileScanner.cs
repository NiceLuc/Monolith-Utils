using System.Text.RegularExpressions;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixComponentFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _wxsRegex = new(@"<Compile Include=""(?<wix_path>.+?\.wxs)""", RegexOptions.Multiline);
    private static readonly Regex _assemblyNameRegex = new(@"File.+?Source=""\$\(.+?\)(?<assembly_name>.+?\.dll)""", RegexOptions.Multiline);

    public async Task ScanAsync(WixProjectRecord wixProject,
        IDictionary<string, ProjectRecord> projectsByAssemblyName,
        CancellationToken cancellationToken)
    {
        // cannot scan a file that does not exist
        if (!wixProject.DoesExist)
            return;

        // open the wix project file to find out what wxs files are required
        var wxsFilePaths = await GetWxsFilePaths(wixProject, cancellationToken);

        // capture all harvested assembly names
        foreach (var wxsFilePath in wxsFilePaths)
        {
            var wxsXml = await fileStorage.ReadAllTextAsync(wxsFilePath, cancellationToken);

            // find all harvested projects associated with this component file
            foreach (Match match in _assemblyNameRegex.Matches(wxsXml))
            {
                var assemblyName = match.Groups["assembly_name"].Value;
                var assemblyNameIndex = assemblyName.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
                if (assemblyNameIndex >= 0)
                    assemblyName = assemblyName[(assemblyNameIndex + 1)..];

                // this is not a project reference, but some other random harvested binary
                if (!projectsByAssemblyName.TryGetValue(assemblyName, out var project))
                    continue;

                // csharp project is required for this wix project (harvested)
                var wixReference = new WixProjectReference(wixProject.Name, true);
                project.WixProjects.Add(wixReference);

                // wix project depends on the csharp project (harvested)
                var reference = new WixProjectReference(project.Name, true);
                wixProject.ProjectReferences.Add(reference);
            }
        }
    }

    private async Task<List<string>> GetWxsFilePaths(WixProjectRecord wixProject, CancellationToken cancellationToken)
    {
        var wixProjectXml = await fileStorage.ReadAllTextAsync(wixProject.Path, cancellationToken);
        var wixDirectory = Path.GetDirectoryName(wixProject.Path)!;

        // find all wxs files required for this wix project
        var wxsFilePaths = new List<string>();
        if (wixProject.IsSdk)
        {
            wxsFilePaths.AddRange(fileStorage.GetFilePaths(wixDirectory, "*.wxs"));
        }
        else
        {
            foreach (Match match in _wxsRegex.Matches(wixProjectXml))
            {
                var relativePath = Path.Combine(wixDirectory, match.Groups["wix_path"].Value);
                var wxsPath = Path.GetFullPath(relativePath);
                wxsFilePaths.Add(wxsPath);
            }
        }

        return wxsFilePaths;
    }
}