using MediatR;
using Microsoft.Extensions.Options;

namespace Deref.Programs;

internal class Initialize
{
    public class Request : IRequest<string>
    {
        public string BranchName { get; set; }
        public string SettingsFilePath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(IOptions<AppSettings> appSettings) : IRequestHandler<Request, string>
    {
        private readonly AppSettings _appSettings = appSettings.Value;

        public Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"AppSettings contains {_appSettings.RequiredSolutions.Length} solutions");
        }
    }
}