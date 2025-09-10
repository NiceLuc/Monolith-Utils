namespace MonoUtils.Domain.Data.Queries;

public abstract class ListQuery : Query
{
    public string? SearchTerm { get; set; }
    public bool ShowListCounts { get; set; }
    public bool ShowListTodos { get; set; }
}