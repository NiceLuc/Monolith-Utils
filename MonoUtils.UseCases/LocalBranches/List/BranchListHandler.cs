using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;

namespace MonoUtils.UseCases.LocalBranches.List;

public class BranchListHandler(ILogger<BranchListHandler> logger, IFileStorage fileStorage) : IRequestHandler<BranchListQuery>
{
    public Task Handle(BranchListQuery request, CancellationToken cancellationToken)
    {
        var branches = fileStorage.GetDirectoryNames(request.RootPath);
        foreach (var branchName in branches)
        {
            var isCurrent = branchName == request.BranchName;
            var statusChar = isCurrent ? "*" : " ";
            logger.LogInformation($"{statusChar} {branchName}");
        }

        logger.LogInformation("Use the -s flag to change branches (eg. 'branch -s {{BRANCH_NAME}}')");
        return Task.CompletedTask;
    }
}