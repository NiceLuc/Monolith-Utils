namespace MonoUtils.Domain.Data.Queries;

public abstract class Query
{
    public FilterType BranchFilter { get; set; }
    public bool IsExcludeTests { get; set; }
}