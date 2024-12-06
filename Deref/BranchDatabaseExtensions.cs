using Deref.Programs;
using MonoUtils.Domain.Data;

namespace Deref;

internal static class BranchDatabaseExtensions
{
    internal static QueryOptions ToQueryOptions(this IQueryRequest request)
    {
        return new QueryOptions
        {
            BranchFilter = request.BranchFilter,
            SearchTerm = request.SearchTerm,
            IsExcludeTests = request.IsExcludeTests,
            IsRecursive = request.IsRecursive,
            ShowListCounts = request.ShowListCounts,
            ShowListTodos = request.ShowListTodos
        };
    }
}