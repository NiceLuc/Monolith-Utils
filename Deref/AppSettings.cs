namespace Deref;

public class AppSettings
{
    private static string ResolveDirectoryPath(string pattern, string branchName) =>
        pattern.Replace("{{BRANCH_NAME}}", branchName);

    public string DefaultBranchName { get; set; }
    public string TFSRootTemplate { get; set; }
    public string TempDirectoryTemplate { get; set; }
    public RequiredSolution[] RequiredSolutions { get; set; }

    public class RequiredSolution
    {
        public string BuildName { get; set; }
        public string SolutionPath { get; set; }
    }

    public string GetTFSRootPath(string branchName = "") => ResolveDirectoryPath(TFSRootTemplate, branchName);

    public string GetTempPath(string branchName = "") => ResolveDirectoryPath(TempDirectoryTemplate, branchName);
}