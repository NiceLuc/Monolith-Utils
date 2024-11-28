namespace Deref.Data;

public record SolutionRecord(string Name, string Path, bool IsRequired, bool DoesExist) 
    : SchemaRecord(Name, Path, IsRequired, DoesExist)
{
    public List<string> Builds { get; set; } = new();
    public List<string> Projects { get; set; } = new();
    public List<string> WixProjects { get; set; } = new();
}