namespace Deref.Data;

public record WixProjectRecord(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
{
    public bool IsSdk { get; set; }
    public bool IsPackageRef { get; set; }

    public List<string> References { get; set; } = new();
    public List<string> ReferencedBy { get; set; } = new();

    public List<string> Solutions { get; set; } = new();
    public List<WixProjectReference> ProjectReferences { get; set; } = new();
}