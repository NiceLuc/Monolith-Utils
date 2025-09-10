namespace MonoUtils.Domain.Data;

public record WixProjectRecord(string Name, string Path, bool DoesExist) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public bool IsSdk { get; set; }
    public bool IsPackageRef { get; set; }
    public bool IsRequired { get; set; }

    public string[] References { get; set; } = [];
    public string[] ReferencedBy { get; set; } = [];

    public string[] Solutions { get; set; } = [];
    public WixProjectReference[] ProjectReferences { get; set; } = [];
}