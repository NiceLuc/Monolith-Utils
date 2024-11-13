using CommandLine;

namespace Deref.Options;

[Verb("branch", HelpText = "Manage which TFS branch root to analyze dynamically.")]
internal class BranchOptions
{
    [Option('s', "set-branch", HelpText = "Change to the branch which has the solutions you want to analyze.")]
    public string BranchName { get; set; }
}