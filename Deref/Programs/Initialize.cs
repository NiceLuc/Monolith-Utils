using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SharedKernel;

namespace Deref.Programs;

public class Initialize
{
    public class Request : IRequest<string>
    {
        public string BranchName { get; set; }
        public string ResultsDirectoryPath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(
        IProgramSettingsBuilder settingsBuilder,
        DefinitionSerializer<BranchSchema> serializer,
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        private static readonly Regex _referenceRegex = new(@"""(?<relative_path>[\.-\\a-zA-Z\d]+\.csproj)""", RegexOptions.Multiline);

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = settingsBuilder.Build(request.BranchName, request.ResultsDirectoryPath);
            ValidateRequest(settings, request);

            var builder = new BranchSchemaBuilder();

            var solutionPaths = settings.BuildSolutions.Select(s
                    => Path.Combine(settings.RootDirectory, s.SolutionPath))
                .Distinct() // some builds are using the same solution file (ie. MVIM2 & UpdateMaxVersionInstaller)
                .Where(path => File.Exists(path!)); // some local branch cloak large directories to save space!

            // add all solutions to the builder
            var solutionTokens = solutionPaths.Select(builder.AddSolution).ToArray();
            foreach (var solutionToken in solutionTokens)
            {
                var solutionFile = await fileStorage.ReadAllTextAsync(solutionToken.Path, cancellationToken);
                var solutionDirectory = Path.GetDirectoryName(solutionToken.Path)!;

                // add all projects to the builder
                foreach (Match match in _referenceRegex.Matches(solutionFile))
                {
                    var projectPath = Path.Combine(solutionDirectory, match.Groups["relative_path"].Value);
                    var projectToken = builder.AddProject(Path.GetFullPath(projectPath));

                    // assign each project to the solution
                    builder.AssignProjectToSolution(solutionToken, projectToken);
                }
            }

            /*
            // go back through all projects and add references
            var filePaths = fileStorage.GetFilePaths(settings.RootDirectory, "*.csproj");
            var projectTokens = filePaths.Select(builder.AddProject).ToArray();
            foreach (var projectToken in projectTokens)
            {
                var projectFile = await fileStorage.ReadAllTextAsync(projectToken.Path, cancellationToken);
                var projectDirectory = Path.GetDirectoryName(projectToken.Path)!;
                foreach (Match match in _referenceRegex.Matches(projectFile))
                {
                    var projectPath = Path.Combine(projectDirectory, match.Value);
                    builder.AddReference(projectToken, projectPath);
                }
            }
            */

            var data = builder.Build();
            var filePath = Path.Combine(settings.TempDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return $"Database contains {data.Projects.Count} project";
        }

        private void ValidateRequest(ProgramSettings settings, Request request)
        {
            if (!fileStorage.DirectoryExists(settings.TempDirectory))
            {
                fileStorage.CreateDirectory(settings.TempDirectory);
                return;
            }

            if (!request.ForceOverwrite)
                throw new InvalidOperationException($"Results directory already exists: {settings.TempDirectory} (use -f to overwrite)");
        }
    }
}