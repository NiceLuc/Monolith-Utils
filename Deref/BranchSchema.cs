﻿namespace Deref;

public class BranchSchema
{
    public List<Project> Projects { get; set; }
    public List<Solution> Solutions { get; set; }

    public abstract record SchemaRecord(string Name, string Path, bool Exists);

    public record Project(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
    {
        public bool IsSdk { get; set; }
        public bool IsNetStandard2 { get; set; }
        public bool IsPackageRef { get; set; }

        public List<string> Solutions { get; set; } = new();
        public List<string> References { get; set; } = new();
        public List<string> ReferencedBy { get; set; } = new();
    }

    public record Solution(string Name, string Path, bool Exists) : SchemaRecord(Name, Path, Exists)
    {
        public List<string> Builds { get; set; } = new();
        public List<string> Projects { get; set; } = new();
    }
}