using MediatR;
using Microsoft.Extensions.Options;
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
        IFileStorage fileStorage) : IRequestHandler<Request, string>
    {
        public Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = settingsBuilder.Build(request.BranchName, request.ResultsDirectoryPath);
            ValidateRequest(settings, request);

            return Task.FromResult($"AppSettings contains {settings.BuildSolutions.Length} solutions");
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