using MediatR;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;

namespace MonoUtils.UseCases.InitializeDatabase;

public class ImportProject
{
    public class Command : IRequest<ProjectRecord>
    {
        public string Path { get; set; }
    }

    public class Handler(
        IBranchDatabaseBuilder builder,
        ScannedFiles scannedFiles,
        StandardProjectFileScanner scanner) : IRequestHandler<Command, ProjectRecord>
    {
        public async Task<ProjectRecord> Handle(Command command, CancellationToken cancellationToken)
        {
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
                var references = new List<string>(results.References.Count);
                foreach (var referencePath in results.References)
                {
                    // NOTE: RECURSION!
                    var request = new Command { Path = referencePath };
                    var reference = await Handle(request, cancellationToken);
                    reference.ReferencedBy = reference.ReferencedBy.Concat([project.Name]).ToArray();

                    builder.AddProjectReference(project, reference);

                    // keep track of all references to create back-mapping later
                    references.Add(reference.Name);
                }

                // step 4: update references now that we have all of their names
                project.References = project.References.Concat(references).ToArray();
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format($"Error importing project: {command.Path} ({ex.Message})");
                builder.AddError(project, errorMessage, ex);
            }

            return project;
        }
    }
}