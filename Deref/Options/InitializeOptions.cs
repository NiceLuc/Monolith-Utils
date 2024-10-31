using CommandLine;

namespace Deref.Options;

[Verb("init", HelpText = "Setup the solution and project database file.")]
internal class InitializeOptions
{
    [Value(0, HelpText = "The name of the branch which has the solutions you want to analyze.")]
    public string BranchName { get; set; }

    [Option('o', "output", HelpText = "The output directory where the meta data files will be written to (default = '').")]
    public string ResultsDirectoryPath { get; set; }

    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing files when the directory already exists.")]
    public bool ForceOverwrite { get; set; }
}