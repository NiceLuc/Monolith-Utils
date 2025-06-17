using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace MonoUtils.UseCases.InitializeDatabase;

public class ImportProject
{
    public class Command : IRequest<ProjectRecord>
    {
        public BranchDatabaseBuilder Builder { get; set; }
        public string Path { get; set; }
    }

    public class Handler(
        ISender sender,
        ILogger<Handler> logger,
        ScannedFiles scannedFiles,
        StandardProjectFileScanner scanner) : IRequestHandler<Command, ProjectRecord>
    {
        public async Task<ProjectRecord> Handle(Command command, CancellationToken cancellationToken)
        {
            var builder = command.Builder;
            var project = builder.GetOrAddProject(command.Path);

            if (!project.DoesExist || scannedFiles.Contains(project.Path))
                return project;

            try
            {
                // step 1: parse the project file and gather all required information
                var results = await scanner.ScanAsync(project.Path, cancellationToken);
                scannedFiles.Add(project.Path);

                // step 2: update the project with the scanned results
                project = project with
                {
                    AssemblyName = results.AssemblyName,
                    PdbFileName = results.PdbFileName,
                    IsSdk = results.IsSdk,
                    IsNetStandard2 = results.IsNetStandard2,
                    IsPackageRef = results.IsPackageRef,
                    IsTestProject = results.IsTestProject,
                };

                builder.UpdateProject(project);

                // step 3: add all project references
                foreach (var referencePath in results.References)
                {
                    // NOTE: RECURSION!
                    var request = new Command
                    {
                        Builder = builder,
                        Path = referencePath
                    };

                    var reference = await sender.Send(request, cancellationToken);
                    project.References.Add(reference.Name);
                    reference.ReferencedBy.Add(project.Name);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format($" - Error importing project: {command.Path} ({ex.Message})");
                logger.LogWarning(errorMessage);
                builder.AddError(project, errorMessage, ErrorSeverity.Critical);
            }

            return project;
        }
    }
}