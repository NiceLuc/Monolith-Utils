using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;

namespace MonoUtils.UseCases.LocalBranches;

public class BranchList
{
    public class Query : IRequest
    {
        /// <summary>
        /// Provides the root path where identical directories begin. This is typically the root of the TFS branch.
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// The name of the branch that is currently defined as the "default" branch name.
        /// </summary>
        public string BranchName { get; set; }
    }


    public class Handler(ILogger<Handler> logger, IFileStorage fileStorage) : IRequestHandler<Query>
    {
        public Task Handle(Query request, CancellationToken cancellationToken)
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
}