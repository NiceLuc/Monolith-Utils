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
        public string BranchName { get; set; }
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
            var settings = await settingsBuilder.BuildAsync(request.BranchName, cancellationToken);
            ValidateRequest(settings, request);

            var builder = databaseBuilderFactory.Create(settings.RequiredBuildSolutions);

            // get all solution files in the mono repo branch
            var solutionFiles = fileStorage.GetFilePaths(settings.TfsRootDirectory, "*.sln");
            foreach (var solutionFilePath in solutionFiles)
            {
                // parse the solution file and gather all project references
                var solutionResults = await solutionFileScanner.ScanAsync(builder, solutionFilePath, cancellationToken);

                // parse each wix project file associated with this solution
                var solution = solutionResults.Solution;
                foreach (var wixProject in solutionResults.WixProjectsToScan) 
                    await wixProjectFileScanner.ScanAsync(builder, solution, wixProject, cancellationToken);
            }

            // scan each project file to capture all details
            var projects = new List<ProjectRecord>();
            foreach (var project in builder.GetProjectFilesToScan())
            {
                await projectFileScanner.ScanAsync(builder, project, cancellationToken);
                projects.Add(project);
            }

            // scan each wix project file one by one.
            // note: we provide the assembly names to scan our wxs files for harvested binaries
            var assemblyNames = projects
                .Where(p => p.DoesExist) // file must exist!
                .Where(p => !p.Path.Contains("Test", StringComparison.OrdinalIgnoreCase)) // ignore test projects
                .ToDictionary(p => p.AssemblyName);

            foreach (var project in builder.GetWixProjectFilesToScan())
                await wixComponentFileScanner.ScanAsync(project, assemblyNames, cancellationToken);

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