namespace MonoUtils.Domain.Data;

public record SolutionRecord(string Name, string Path, bool DoesExist) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public bool IsRequired { get; set; }
    public string[] Builds { get; set; } = [];
    public SolutionProjectReference[] Projects { get; set; } = [];
    public string[] WixProjects { get; set; } = [];
}