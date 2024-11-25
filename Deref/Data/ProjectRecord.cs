namespace Deref.Data;

public record ProjectRecord(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
{
    public string AssemblyName { get; set; }
    public string PdbFileName { get; set; }
    public bool IsSdk { get; set; }
    public bool IsNetStandard2 { get; set; }
    public bool IsPackageRef { get; set; }

    public List<string> Solutions { get; set; } = new();
    public List<string> References { get; set; } = new();
    public List<string> ReferencedBy { get; set; } = new();
    public List<WixProjectReference> WixProjects { get; set; } = new();
}