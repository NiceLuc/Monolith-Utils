namespace Deref;

public class BranchDatabase
{
    public List<Project> Projects { get; set; }
    public List<Solution> Solutions { get; set; }
    public List<WixProj> WixProjects { get; set; }

    public abstract record SchemaRecord(string Name, string Path, bool Exists);

    public record Project(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
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

    public record Solution(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
    {
        public List<string> Builds { get; set; } = new();
        public List<string> Projects { get; set; } = new();
        public List<string> WixProjects { get; set; } = new();
    }

    public record WixProj(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
    {
        public bool IsSdk { get; set; }
        public bool IsPackageRef { get; set; }

        public List<string> References { get; set; } = new();
        public List<string> ReferencedBy { get; set; } = new();

        public List<string> Solutions { get; set; } = new();
        public List<WixProjectReference> ProjectReferences { get; set; } = new();
    }

    public record WixProjectReference(string ProjectName, bool IsHarvested);
}