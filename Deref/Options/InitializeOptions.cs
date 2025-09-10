using CommandLine;

namespace MonoUtils.App.Options;

[Verb("init", HelpText = "Setup the solution and project database file.")]
internal class InitializeOptions
{
    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing files when the directory already exists.")]
    public bool ForceOverwrite { get; set; }
}