using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace MonoUtils.UseCases.InitializeDatabase;

public class ImportSolution 
{
    public class Command : IRequest<SolutionRecord>
    {
        public BranchDatabaseBuilder Builder { get; set; }
        public string Path { get; set; }
        public string[] BuildNames { get; set; }
    }

    public class Handler(
        ISender sender,
        ILogger<Handler> logger,
        ScannedFiles scannedFiles,
        SolutionFileScanner scanner) : IRequestHandler<Command, SolutionRecord>
    {
        public async Task<SolutionRecord> Handle(Command command, CancellationToken cancellationToken)
        {
            var builder = command.Builder;
            var solution = builder.GetOrAddSolution(command.Path);

            if (!solution.DoesExist || scannedFiles.Contains(command.Path))
                return solution;

            try
            {
                // step 1: parse the solution file and gather all required information
                var results = await scanner.ScanAsync(solution.Path, cancellationToken);
                scannedFiles.Add(solution.Path);

                solution.Builds.AddRange(command.BuildNames);

                // step 2: add all project and wix project references
                foreach (var item in results.Projects)
                {
                    var request = new ImportProject.Command
                    {
                        Builder = builder,
                        Path = item.Path
                    };

                    var project = await sender.Send(request, cancellationToken);
                    project.Solutions.Add(solution.Name);
                    solution.Projects.Add(new SolutionProjectReference(project.Name, item.Type));
                }

                // step 3: scan each wix project (recursively through each wxs file)
                var projects = builder.GetProjectsAvailableForInstallers(solution);
                foreach (var path in results.WixProjects)
                {
                    var request = new ImportWixProject.Request
                    {
                        Builder = builder,
                        Path = path,
                        AvailableProjects = projects
                    };

                    var wixProject = await sender.Send(request, cancellationToken);
                    wixProject.Solutions.Add(solution.Name);
                    solution.WixProjects.Add(wixProject.Name);
                }
            }
            catch (Exception e)
            {
                var errorMessage = string.Format($" - Error scanning solution: {command.Path} ({e.Message})");
                logger.LogWarning(errorMessage);
                builder.AddError(solution, errorMessage, ErrorSeverity.Critical);
            }

            return solution;
        }
    }
}