namespace MonoUtils.Domain.Data;

public record ProjectRecord(string Name, string Path, bool DoesExist) 
    : SchemaRecord(Name, Path, DoesExist)
{
    public bool IsSdk { get; set; }
    public bool IsNetStandard2 { get; set; }
    public bool IsPackageRef { get; set; }
    public bool IsTestProject { get; set; }
    public bool IsRequired { get; set; }

    public string AssemblyName { get; set; }
    public string PdbFileName { get; set; }

    public string[] Solutions { get; set; } = [];
    public string[] References { get; set; } = [];
    public string[] ReferencedBy { get; set; } = [];
    public WixProjectReference[] WixProjects { get; set; } = [];
}