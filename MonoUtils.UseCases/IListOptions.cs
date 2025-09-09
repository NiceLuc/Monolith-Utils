using MonoUtils.Domain.Data;

namespace MonoUtils.UseCases;

public interface IListOptions
{
    string? SearchTerm { get; set; }
    bool IsExcludeTests { get; set; }
    FilterType FilterBy { get; set; }
}