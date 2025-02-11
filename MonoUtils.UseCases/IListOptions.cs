using MonoUtils.Domain.Data;

namespace MonoUtils.UseCases;

public interface IListOptions
{
    bool IsExcludeTests { get; set; }
    string? SearchTerm { get; set; }
    FilterType FilterBy { get; set; }
    bool IsRecursive { get; set; }
}