namespace MonoUtils.Domain.Data;

public record SolutionRecord(string Name, string Path, bool IsRequired, bool DoesExist) 
    : SchemaRecord(Name, Path, IsRequired, DoesExist)
{
    public List<string> Builds { get; set; } = new();
    public List<ProjectReference> Projects { get; set; } = new();
    public List<string> WixProjects { get; set; } = new();
}

public enum ProjectTypes { Unknown, OldStyle, SdkStyle }

public record ProjectReference(string Name, ProjectTypes Type);
