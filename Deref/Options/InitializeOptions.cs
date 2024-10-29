using CommandLine;

namespace Deref.Options;

internal class InitializeOptions
{
    [Option('b', "branch-name", HelpText = "The name of the branch which has the solutions you want to analyze.")]
    public string BranchName { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output file name of the meta data file (default = '').")]
    public string SettingsFilePath { get; set; }

    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing file.")]
    public bool ForceOverwrite { get; set; }
}