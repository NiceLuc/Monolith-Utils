using MediatR;

namespace MonoUtils.UseCases.LocalBranches;

public static class BranchSet
{
    public class Request : IRequest { }

    public class Handler : IRequestHandler<Request>
    {
        public Task Handle(Request request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}