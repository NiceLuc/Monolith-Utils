using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace MonoUtils.UseCases.InitializeDatabase;

public class ImportWixProject
{
    public class Request : IRequest<WixProjectRecord>
    {
        public IBranchDatabaseBuilder Builder { get; set; }
        public string Path { get; set; }
        public ProjectRecord[] AvailableProjects { get; set; } = [];
    }

    public class Handler(
        ILogger<Handler> logger,
        ScannedFiles scannedFiles,
        WixProjectFileScanner wixScanner,
        WixComponentFileScanner wxsScanner) : IRequestHandler<Request, WixProjectRecord>
    {
        public async Task<WixProjectRecord> Handle(Request request, CancellationToken cancellationToken)
        {
            var builder = request.Builder;
            var wix = builder.GetOrAddWixProject(request.Path);
            if (!wix.DoesExist || scannedFiles.Contains(request.Path))
                return wix;

            try
            {
                var results = await wixScanner.ScanAsync(wix.Path, cancellationToken);
                scannedFiles.Add(wix.Path);

                wix = wix with
                {
                    IsSdk = results.IsSdk,
                    IsPackageRef = results.IsPackageRef,
                };

                builder.UpdateWixProject(wix);

                // capture project harvested project references
                var projectPaths = request.AvailableProjects.ToDictionary(p => p.Path, StringComparer.OrdinalIgnoreCase);
                foreach (var path in results.ProjectReferences)
                {
                    if (!projectPaths.TryGetValue(path, out var project))
                    {
                        builder.AddWarning(wix, "Project is not available for this wix file");
                        continue;
                    }

                    builder.AddWixProjectReference(wix, project, false);

                    if (!project.DoesExist)
                    {
                        builder.AddWarning(wix, $"Wix project file ({wix.Path}) references a project that does not exist: {project.Path}");
                    }
                }

                if (!results.ComponentFilePaths.Any())
                    return wix;

                // capture manually harvested project assemblies
                var assemblyNames = request.AvailableProjects.ToDictionary(p => p.AssemblyName, StringComparer.OrdinalIgnoreCase);
                var harvested = new HashSet<string>();
                foreach (var path in results.ComponentFilePaths)
                {
                    var wxsResults = await wxsScanner.ScanAsync(path, cancellationToken);
                    foreach (var assemblyName in wxsResults.AssemblyNames)
                    {
                        if (!assemblyNames.TryGetValue(assemblyName, out var project))
                            continue;

                        // prevent duplicate references
                        if (!harvested.Add(assemblyName))
                        {
                            builder.AddWarning(wix, $"Duplicate manually harvested assembly ({assemblyName}) added to {wix.Path}");
                            continue;
                        }

                        builder.AddWixProjectReference(wix, project, true);
                        if (!project.DoesExist)
                        {
                            builder.AddWarning(wix, $"Wix component file ({path}) references a project assembly that does not exist: {assemblyName} ({project.Path})");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = string.Format($" - Error scanning Wix project: {request.Path} ({e.Message})");
                logger.LogWarning(errorMessage);
                builder.AddError(wix, "Error importing wix project", e);
            }

            return wix;
        }
    }
}