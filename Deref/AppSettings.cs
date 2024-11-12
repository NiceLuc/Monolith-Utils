namespace Deref;

public class AppSettings
{
    public string DefaultBranchName { get; set; }
    public string TFSRootTemplate { get; set; }
    public string TempDirectoryTemplate { get; set; }
    public RequiredSolution[] RequiredSolutions { get; set; }

    public class RequiredSolution
    {
        public string BuildName { get; set; }
        public string SolutionPath { get; set; }
    }
}