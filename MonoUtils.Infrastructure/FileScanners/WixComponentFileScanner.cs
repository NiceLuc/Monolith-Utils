using System.Text.RegularExpressions;
using MonoUtils.Domain;

namespace MonoUtils.Infrastructure.FileScanners;

public class WixComponentFileScanner(IFileStorage fileStorage)
{
    private static readonly Regex _assemblyNameRegex = new(@"File.+?Source=""\$\(.+?\)(?<assembly_name>.+?\.dll)""", RegexOptions.Multiline);

    public async Task<Results> ScanAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileStorage.FileExists(path))
            throw new FileNotFoundException($"Wxs file not found: {path}");

        // find all harvested projects associated with this component file
        var wxsXml = await fileStorage.ReadAllTextAsync(path, cancellationToken);

        var assemblyNames = new List<string>();
        foreach (Match match in _assemblyNameRegex.Matches(wxsXml))
        {
            var assemblyName = match.Groups["assembly_name"].Value;
            var assemblyNameIndex = assemblyName.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
            if (assemblyNameIndex >= 0)
                assemblyName = assemblyName[(assemblyNameIndex + 1)..];

            assemblyNames.Add(assemblyName);
        }

        return new Results
        {
            AssemblyNames = assemblyNames
        };
    }

    public class Results
    {
        public List<string> AssemblyNames { get; set; }
    }
}