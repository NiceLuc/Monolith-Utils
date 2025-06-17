namespace MonoUtils.Domain.Data;

public record WixProjectRecord(string Name, string Path, bool DoesExist) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public bool IsSdk { get; set; }
    public bool IsPackageRef { get; set; }
    public bool IsRequired { get; set; }

    public List<string> References { get; set; } = new();
    public List<string> ReferencedBy { get; set; } = new();

    public List<string> Solutions { get; set; } = new();
    public List<WixProjectReference> ProjectReferences { get; set; } = new();
}