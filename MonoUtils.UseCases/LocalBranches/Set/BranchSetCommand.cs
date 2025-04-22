using MediatR;

namespace MonoUtils.UseCases.LocalBranches.Set;

public class BranchSetCommand : IRequest
{

}

public class BranchSetHandler : IRequestHandler<BranchSetCommand>
{
    public Task Handle(BranchSetCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}