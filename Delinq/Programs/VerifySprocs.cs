using MediatR;

namespace Delinq.Programs;

public sealed class VerifySprocs
{
    public record Request : IRequest<string>
    {
        public string RepositoryFilePath { get; set; }
        public string ConnectionString { get; set; }
        public string SettingsFilePath { get; set; }
    }

    public class Handler : IRequestHandler<Request, string>
    {
        public Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Request: {request}");
            return Task.FromResult("Implement me!!");
        }
    }
}