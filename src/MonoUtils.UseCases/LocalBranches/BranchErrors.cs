using SharedKernel;

namespace MonoUtils.UseCases.LocalBranches;

public static class BranchErrors
{
    public static Error BranchNotFound(string message) => new("Branch.NotFound", message);
    public static Error InvalidRequest(string message) => new("Branch.InvalidRequest", message);
    public static Error BranchNotChanged(string message) => new("Branch.NotChanged", message);
}