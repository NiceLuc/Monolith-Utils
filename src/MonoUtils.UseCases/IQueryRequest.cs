using MonoUtils.Domain.Data;

namespace MonoUtils.UseCases;

public interface IQueryRequest
{
    FilterType BranchFilter { get; set; }
    string? SearchTerm { get; set; }
    bool IsExcludeTests { get; set; }
    bool IsRecursive { get; set; }
    bool ShowListCounts { get; set; }
    bool ShowListTodos { get; set; }
}