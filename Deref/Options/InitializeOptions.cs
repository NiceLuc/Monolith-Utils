using CommandLine;

namespace Deref.Options;

[Verb("init", HelpText = "Setup the solution and project database file.")]
internal class InitializeOptions
{
    [Option('b', "branch", HelpText = "The name of the branch which has the solutions you want to analyze.")]
    public string BranchName { get; set; }

    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing files when the directory already exists.")]
    public bool ForceOverwrite { get; set; }
}