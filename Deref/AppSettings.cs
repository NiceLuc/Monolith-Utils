namespace Deref;

public class AppSettings
{
    public string TFSRootTemplate { get; set; }
    public string DefaultBranchName { get; set; }
    public string TempDirectoryTemplate { get; set; }
    public string MetaDataFileNameTemplate { get; set; }
    public RequiredSolution[] RequiredSolutions { get; set; }

    public class RequiredSolution
    {
        public string BranchName { get; set; }
        public string SolutionPath { get; set; }
    }
}