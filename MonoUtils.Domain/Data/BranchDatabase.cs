namespace MonoUtils.Domain.Data;

public class BranchDatabase
{
    public List<ProjectRecord> Projects { get; set; }
    public List<SolutionRecord> Solutions { get; set; }
    public List<WixProjectRecord> WixProjects { get; set; }
}