namespace MonoUtils.Domain.Data;

public class QueryOptions
{
    public FilterType BranchFilter { get; set; } // All, OnlyRequired, OnlyNonRequired
    public string? SearchTerm { get; set; }
    public bool IsExcludeTests { get; set; }
    public bool IsRecursive { get; set; }
    public bool ShowListCounts { get; set; }
    public bool ShowListTodos { get; set; }

}