using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace Deref.Programs;

public class Initialize
{
    public class Request : IRequest<string>
    {
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(
        ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        BranchDatabaseBuilderFactory databaseBuilderFactory,
        SolutionFileScanner solutionFileScanner,
        WixProjectFileScanner wixProjectFileScanner,
        StandardProjectFileScanner projectFileScanner,
        WixComponentFileScanner wixComponentFileScanner,
        IDefinitionSerializer<BranchDatabase> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            ValidateRequest(settings, request);

            var builder = databaseBuilderFactory.Create(settings.RequiredBuildSolutions);

            // get all solution files in the mono repo branch
            logger.LogInformation("Finding all solution files in {TfsRootDirectory} directory...", settings.TfsRootDirectory);
            var solutionFiles = fileStorage.GetFilePaths(settings.TfsRootDirectory, "*.sln");
            logger.LogInformation("Found {SolutionCount} files!", solutionFiles.Length);

            // remove specific directories that we don't care about!
            var total = solutionFiles.Length;
            if (settings.DirectoriesToIgnore.Length > 0)
            {
                foreach (var directory in settings.DirectoriesToIgnore)
                {
                    solutionFiles = solutionFiles.Where(f => !f.StartsWith(directory, StringComparison.OrdinalIgnoreCase)).ToArray();

                    var removedCount = total - solutionFiles.Length;
                    total = solutionFiles.Length;

                    logger.LogInformation(" - Ignoring {RemovedCount} solutions in {IgnoredDirectory}", removedCount, directory);
                }

                logger.LogInformation("Filtered {SolutionCount} files!", solutionFiles.Length);
            }

            // scan each solution file to capture all details
            var count = 0;
            foreach (var solutionFilePath in solutionFiles)
            {
                count += 1;

                try
                {
                    // parse the solution file and gather all project references
                    logger.LogInformation("Scanning {SolutionPath}... ({count} of {total})", solutionFilePath, count, total);
                    var solutionResults = await solutionFileScanner.ScanAsync(builder, solutionFilePath, cancellationToken);
                    var solution = solutionResults.Solution;

                    logger.LogDebug(" - Found {ProjectCount} projects", solution.Projects.Count);
                    if (solutionResults.WixProjectsToScan.Count > 0)
                    {
                        // parse each wix project file associated with this solution
                        foreach (var wixProject in solutionResults.WixProjectsToScan)
                        {
                            logger.LogInformation(" - Scanning {WixProjectPath}...", wixProject.Path);
                            await wixProjectFileScanner.ScanAsync(builder, solution, wixProject, cancellationToken);
                            logger.LogDebug("   - Done");
                        }
                    }
                    logger.LogDebug(" - Done");
                }
                catch (Exception e)
                {
                    var errorMessage = string.Format($" - Error scanning solution: {solutionFilePath} ({e.Message})");
                    logger.LogWarning(errorMessage);
                    builder.AddError(errorMessage);
                }
            }

            logger.LogInformation("---");

            // scan each project file gathered from solutions and wix project to capture all details
            total = builder.ProjectFilesToScanCount;
            count = 0;

            logger.LogInformation("Found {ProjectCount} projects from scans!", total);

            var projects = new List<ProjectRecord>();
            foreach (var project in builder.GetProjectFilesToScan())
            {
                count += 1;

                try
                {
                    logger.LogInformation("Scanning {ProjectPath}... ({count} of {total})", project.Path, count, total);
                    await projectFileScanner.ScanAsync(builder, project, cancellationToken);
                    logger.LogDebug(" - Done");

                    // capture the project so that we can use it to scan for harvested binaries
                    projects.Add(project);
                }
                catch (Exception e)
                {
                    var errorMessage = string.Format($" - Error scanning project: {project.Path} ({e.Message})");
                    logger.LogWarning(errorMessage);
                    builder.AddError(errorMessage);
                }
            }

            // scan each wix project file one by one.

            logger.LogInformation("---");

            // scan each WiX project file to capture all harvesting details
            total = builder.WixProjectFilesToScanCount;
            count = 0;

            logger.LogInformation("Found {WixProjectCount} wix projects from scans!", total);

            foreach (var project in builder.GetWixProjectFilesToScan())
            {
                try
                {
                    count += 1;

                    logger.LogInformation("Scanning wxs files from {WixProjectPath}... ({count} of {total})", project.Path, count, total);
                    await wixComponentFileScanner.ScanAsync(builder, project, cancellationToken);
                    logger.LogDebug(" - Done");
                }
                catch (Exception e)
                {
                    var errorMessage = string.Format($" - Error scanning wix project: {project.Path} ({e.Message})");
                    logger.LogWarning(errorMessage);
                    builder.AddError(errorMessage);
                }
            }

            // persist the results to a json file
            var data = builder.CreateDatabase();

            var filePath = Path.Combine(settings.TempRootDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return filePath;
        }

        private void ValidateRequest(ProgramSettings settings, Request request)
        {
            if (!fileStorage.DirectoryExists(settings.TempRootDirectory))
            {
                fileStorage.CreateDirectory(settings.TempRootDirectory);
                return;
            }

            if (!request.ForceOverwrite)
                throw new InvalidOperationException(
                    $"Results directory already exists: {settings.TempRootDirectory} (use -f to overwrite)");
        }
    }
}