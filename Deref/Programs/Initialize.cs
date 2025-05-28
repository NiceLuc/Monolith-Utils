using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;
using Serilog.Data;
using SharedKernel;

namespace Deref.Programs;

public class Initialize
{
    public class Request : IRequest<Result>
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
        IFileStorage fileStorage) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            ValidateRequest(settings, request);

            var scanned = new HashSet<string>();
            var wxsFiles = new List<string>();

            var solutionFiles = GetSolutionFilePaths(settings);
            var total = solutionFiles.Length;

            var builder = databaseBuilderFactory.Create(settings.RequiredBuildSolutions);

            // scan each solution file to capture all details
            var count = 0;
            foreach (var solutionFilePath in solutionFiles)
            {
                count += 1;

                // has it already been scanned?
                if (!scanned.Add(solutionFilePath))
                    continue;

                var solution = builder.GetOrAddSolution(solutionFilePath);
                if (!solution.DoesExist)
                {
                    logger.LogError($"Solution file does not exist: {solution.Path}");
                    continue;
                }

                try
                {
                    // parse the solution file and gather all project references
                    logger.LogInformation("Scanning {SolutionPath}... ({count} of {total})", solutionFilePath, count, total);
                    var solutionResults = await solutionFileScanner.ScanAsync(solution, cancellationToken);

                    var transitives = new List<string>();
                    foreach (var projectItem in solutionResults.Projects)
                    {
                        // add the project and give a reference to the solution
                        var project = builder.GetOrAddProject(projectItem.Path);
                        builder.AddSolutionProject(solution, project, projectItem.Type);

                        if (!scanned.Add(projectItem.Path))
                            continue;

                        if (!project.DoesExist)
                        {
                            logger.LogError($"Project file does not exist: {project.Path}");
                            continue;
                        }

                        var projectResults = await projectFileScanner.ScanAsync(project, cancellationToken);
                        transitives.AddRange(projectResults.References);
                    }

                    // todo: resolve all references that are required for each project in this solution
                    ResolveTransitiveReferences(builder, scanned, transitives);

                    // todo: recursively map references
                    foreach (var wixProjectPath in solutionResults.WixProjects)
                    {
                        var wixProject = builder.GetOrAddWixProject(wixProjectPath);
                        builder.AddSolutionWixProject(solution, wixProject);

                        if (!scanned.Add(wixProjectPath))
                            continue;

                        if (!wixProject.DoesExist)
                        {
                            logger.LogError($"Wix project file does not exist: {wixProject.Path}");
                            continue;
                        }

                        var wixProjectResults = await wixProjectFileScanner.ScanAsync(wixProject, cancellationToken);
                        foreach (var projectPath in wixProjectResults.ProjectReferences)
                        {
                            var project = builder.GetOrAddProject(projectPath);
                            builder.AddWixProjectReference(wixProject, project);

                            if (!scanned.Contains(projectPath))
                            {
                                logger.LogError($"Wix project file ({wixProject.Path}) references a project that has not been scanned: {project.Path}");
                                continue;
                            }

                            if (!project.DoesExist)
                            {
                                logger.LogError($"Wix project file ({wixProject.Path}) references a project that does not exist: {project.Path}");
                                continue;
                            }
                        }

                        // add all component files to be scanned at the end
                        wxsFiles.AddRange(wixProjectResults.ComponentFilePaths);
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

                /*
            var reference = new ProjectReference(project.Name, projectType);
            project.Solutions.Add(solution.Name);
            solution.Projects.Add(reference);

                    // when passing true for isRequired parameter, we need to make sure the flag is set
                    // if the flag is not set, then we must add a new "required" instance of the project
                    // important note: once a project has been defined as required, it's always required
                    if (isRequired && !project.IsRequired)
                    {
                        var requiredProject = new ProjectRecord(project.Name, project.Path, project.DoesExist);
                        _projectsByName[project.Name] = requiredProject;
                        _projectsByPath[project.Path] = requiredProject;
                        project = requiredProject;
                    }

            project.References.Add(reference.Name);
            reference.ReferencedBy.Add(project.Name);
                 */
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

        /*

                wixProject.Solutions.Add(solution.Name);
                solution.WixProjects.Add(wixProject.Name);

                // wix projects get returned to the caller
                results.WixProjectsToScan.Add(wixProject);
                results.WixProjects.Add(wixProjectRef);


            // get a reference to the csharp project
            var project = builder.GetOrAddProject(projectPath, solution.IsRequired);

            // csharp project is required for the wix project (not harvested)
            var wixReference = new WixProjectReference(wixProject.Name, false);
            project.WixProjects.Add(wixReference);

            // wix project depends on the csharp project (not harvested)
            var projectReference = new WixProjectReference(project.Name, false);
            wixProject.ProjectReferences.Add(projectReference);
         */
                try
                {
                    count += 1;

                    logger.LogInformation("Scanning wxs files from {WixProjectPath}... ({count} of {total})", project.Path, count, total);
                    var projects = builder.GetProjectsAvailableForWix(wixProject);
                    /*
        // cannot scan a file that does not exist
        if (!wixProject.DoesExist)
            return;

        if (wixProject.Solutions.Count != 1)
            throw new InvalidOperationException($"Wix project ({wixProject.Path}) is referenced by {wixProject.Solutions.Count} project(s)");

        // open the wix project file to find out what wxs files are required
        var wxsFilePaths = await GetWxsFilePaths(wixProject, cancellationToken);

        // capture all harvested assembly names
        foreach (var wxsFilePath in wxsFilePaths)
        {


            var wixReference = new WixProjectReference(wixProject.Name, true);
            project.WixProjects.Add(wixReference);

            // wix project depends on the csharp project (harvested)
            var reference = new WixProjectReference(project.Name, true);
            wixProject.ProjectReferences.Add(reference);
        }
                     */
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
            return Result.Success();
        }

        private string[] GetSolutionFilePaths(ProgramSettings settings)
        {
            // get all solution files in the mono repo branch
            logger.LogInformation("Finding all solution files in {TfsRootDirectory} directory...", settings.TfsRootDirectory);
            var solutionFiles = fileStorage.GetFilePaths(settings.TfsRootDirectory, "*.sln");
            logger.LogInformation("Found {SolutionCount} files!", solutionFiles.Length);

            // remove specific directories that we don't care about!
            var total = solutionFiles.Length;
            if (settings.DirectoriesToIgnore.Length > 0)
            {
                foreach (var ignorePath in settings.DirectoriesToIgnore)
                {
                    solutionFiles = solutionFiles.Where(f => !f.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase)).ToArray();

                    var removedCount = total - solutionFiles.Length;
                    total = solutionFiles.Length;

                    logger.LogInformation(" - Ignoring {RemovedCount} solutions in {IgnoredDirectory}", removedCount, ignorePath);
                }

                logger.LogInformation("Filtered {SolutionCount} files!", solutionFiles.Length);
            }

            // we want to return the solutions in order by REQUIRED first
            var requiredSolutions = settings.RequiredBuildSolutions
                .Select(b => b.SolutionPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var solutions = solutionFiles
                .Where(solution => requiredSolutions.Contains(solution))
                .Select(solution => new
                {
                    Ordinal = requiredSolutions.Contains(solution) ? 0 : 1,
                    Path = solution
                });

            return solutions
                .OrderBy(s => s.Ordinal)
                .Select(s => s.Path)
                .ToArray();
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