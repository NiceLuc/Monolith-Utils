namespace MonoUtils.Domain.Data;

public record SolutionRecord(string Name, string Path, bool DoesExist) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public bool IsRequired { get; set; }
    public List<string> Builds { get; set; } = new();
    public List<SolutionProjectReference> Projects { get; set; } = new();
    public List<string> WixProjects { get; set; } = new();
}