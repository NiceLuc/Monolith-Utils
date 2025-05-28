namespace MonoUtils.Domain.Data;

public record SolutionRecord(string Name, string Path, bool DoesExist, bool IsRequired) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public List<string> Builds { get; set; } = new();
    public List<ProjectReference> Projects { get; set; } = new();
    public List<string> WixProjects { get; set; } = new();
}