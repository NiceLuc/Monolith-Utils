using MediatR;

namespace Deref.Programs;

public interface IQueryRequest : IRequest<string>
{
    FilterType BranchFilter { get; set; }
    string? SearchTerm { get; set; }
    bool IsExcludeTests { get; set; }
    bool IsRecursive { get; set; }
    bool ShowListCounts { get; set; }
    bool ShowListTodos { get; set; }
}
