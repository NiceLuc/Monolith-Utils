namespace MonoUtils.Domain.Data.Queries;

public abstract class ItemQuery<T> : Query where T : SchemaRecord
{
    public string ItemKey { get; set; } = string.Empty;

    public string? ListSearchTerm { get; set; }
    public bool IsRecursive { get; set; }
    public bool ShowListCounts { get; set; }
    public bool ShowListTodos { get; set; }

    public bool IsDescending { get; set; }
    public abstract bool IsActive(T item);
}