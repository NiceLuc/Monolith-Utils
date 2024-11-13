namespace Deref;

public class ProgramSettings
{
    public string BranchName { get; set; }
    public string RootDirectory { get; set; }
    public string TempDirectory { get; set; }
    public BuildDefinition[] BuildSolutions { get; set; }
}