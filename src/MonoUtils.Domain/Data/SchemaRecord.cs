namespace MonoUtils.Domain.Data;

public abstract record SchemaRecord(string Name, string Path, bool DoesExist)
{
    public string[] Errors { get; set; } = [];
}