namespace Deref.Data;

public record SolutionRecord(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
{
    public List<string> Builds { get; set; } = new();
    public List<string> Projects { get; set; } = new();
    public List<string> WixProjects { get; set; } = new();
}