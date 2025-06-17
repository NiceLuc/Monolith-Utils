namespace MonoUtils.Domain.Data;

public class BranchDatabase
{
    public IList<ErrorRecord> Errors { get; set; }

    public IList<SolutionRecord> Solutions { get; set; }
    public IList<ProjectRecord> Projects { get; set; }
    public IList<WixProjectRecord> WixProjects { get; set; }

    public IList<SolutionProjectReference> SolutionProjects { get; set; }
    public IList<ProjectReference> ProjectReferences { get; set; }
    public IList<SolutionWixProjectReference> SolutionWixProjects { get; set; }
    public IList<WixProjectReference> WixProjectReferences { get; set; }
}