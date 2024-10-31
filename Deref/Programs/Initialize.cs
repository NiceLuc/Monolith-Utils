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
        private static readonly Regex _referenceRegex = new(@"[\.-\\a-zA-Z\d]+\.csproj", RegexOptions.Multiline);

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = settingsBuilder.Build(request.BranchName, request.ResultsDirectoryPath);
            ValidateRequest(settings, request);

            var builder = new BranchSchemaBuilder(settings.RootDirectory);

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

            var solutions = fileStorage.GetFilePaths(settings.RootDirectory, "*.sln");
            var solutionTokens = solutions.Select(builder.AddSolution).ToArray();
            foreach (var solutionToken in solutionTokens)
            {
                var solutionFile = await fileStorage.ReadAllTextAsync(solutionToken.Path, cancellationToken);
                var solutionDirectory = Path.GetDirectoryName(solutionToken.Path)!;
                foreach (Match match in _referenceRegex.Matches(solutionFile))
                {
                    var projectPath = Path.Combine(solutionDirectory, match.Value);
                    builder.AssignProjectToSolution(solutionToken, projectPath);
                }
            }

            var data = builder.Build();
            var filePath = Path.Combine(settings.TempDirectory, "db.json");
            await serializer.SerializeAsync(filePath, data, cancellationToken);
            return $"Database contains {data.Projects.Count} project";
        }

        private void ValidateRequest(ProgramSettings settings, Request request)
        {
            if (fileStorage.DirectoryExists(settings.TempDirectory))
            {
                if (!request.ForceOverwrite)
                    throw new InvalidOperationException(
                        $"Results directory already exists: {settings.TempDirectory} (use -f to overwrite)");
            }
            else
                fileStorage.CreateDirectory(request.ResultsDirectoryPath);
        }
    }
}