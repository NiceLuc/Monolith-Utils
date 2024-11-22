using MediatR;

namespace Deref.Programs;

public interface IBranchDatabaseQueryRequest : IRequest<string>
{
    FilterType BranchFilter { get; set; }
    string? SearchTerm { get; set; }
    bool IsExcludeTests { get; set; }
    bool IsIncludeAll { get; set; }
    bool IsIncludeOnlyRequired { get; set; }
    bool IsIncludeOnlyNonRequired { get; set; }
    bool IsRecursive { get; set; }
}