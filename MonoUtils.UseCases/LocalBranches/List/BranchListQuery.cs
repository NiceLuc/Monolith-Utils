using MediatR;

namespace MonoUtils.UseCases.LocalBranches.List;

public class BranchListQuery : IRequest
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