using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.UseCases.InitializeDatabase;
using SharedKernel;

namespace Deref.Programs;

public class Initialize
{
    public class Request : IRequest<Result>
    {
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(
        ISender sender,
        ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        BranchDatabaseBuilderFactory builderFactory,
        IDefinitionSerializer<BranchDatabase> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            ValidateRequest(settings, request);

            // assign all builds to their respective solutions
            var builder = builderFactory.Create();

            // now scan each solution
            var items = GetSolutionItemsInOrder(settings);
            var solutionFileCount = items.Length;

            var count = 0;
            foreach (var item in items)
            {
                count += 1;

                logger.LogInformation("Scanning {SolutionPath}... ({count} of {total})", item.Path, count, solutionFileCount);

                var command = new ImportSolution.Command
                {
                    Builder = builder,
                    Path = item.Path, 
                    BuildNames = item.BuildNames
                };

                var solution = await sender.Send(command, cancellationToken);
                logger.LogInformation($"Scanned {solution.Path} ({solution.Projects.Length} projects, {solution.WixProjects} wix projects)");
            }

            // persist the results to a json file
            var data = builder.CreateDatabase();
            var filePath = Path.Combine(settings.TempRootDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return Result.Success();
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

        private SolutionItem[] GetSolutionItemsInOrder(ProgramSettings settings)
        {
            // get all solution files in the mono repo branch
            logger.LogInformation("Finding all solution files in {TfsRootDirectory} directory...", settings.TfsRootDirectory);
            var paths = fileStorage.GetFilePaths(settings.TfsRootDirectory, "*.sln");
            logger.LogInformation("Found {SolutionCount} files!", paths.Length);

            // remove specific directories that we don't care about!
            if (settings.DirectoriesToIgnore.Length > 0)
            {
                paths = settings.DirectoriesToIgnore.Aggregate(paths,
                    (current, ignorePath) => current.Where(f => !f.StartsWith(ignorePath, StringComparison.OrdinalIgnoreCase)).ToArray());

                logger.LogInformation("Returning {SolutionCount} files!", paths.Length);
            }

            // we want to return each solution with an (optional) array of build names
            var solutions = (from item in settings.RequiredBuilds
                        group item by item.SolutionPath into g
                        select new
                        {
                            SolutionPath = g.Key,
                            BuildDefinitions = g.Select(b => b.BuildName).ToArray()
                        }).ToDictionary(x => x.SolutionPath, x => x.BuildDefinitions);

            // we want to order the results by those with build definitions first, then by path
            return (from path in paths
                    let builds = solutions.TryGetValue(path, out var found) ? found : []
                    select new SolutionItem(path, builds))
                .OrderByDescending(i => i.BuildNames.Length)
                .ThenBy(s => s.Path)
                .ToArray();
        }

        private record struct SolutionItem(string Path, string[] BuildNames);
    }
}