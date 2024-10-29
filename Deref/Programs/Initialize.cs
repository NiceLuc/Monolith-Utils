using MediatR;

namespace Deref.Programs
{
    internal class Initialize
    {
        public class Request : IRequest<string>
        {
            public string BranchName { get; set; }
            public string SettingsFilePath { get; set; }
            public bool ForceOverwrite { get; set; }
        }

        public class Handler : IRequestHandler<Request, string>
        {
            public Task<string> Handle(Request request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
