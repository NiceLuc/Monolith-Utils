using CommandLine;

namespace GVLinQOptimizer.Options;

[Verb("init", HelpText = "Initialize settings file from designer file.")]
internal class InitializeOptions
{
    [Value(0, Required = true, HelpText = "Full path to a legacy DBML file.")]
    public string DbmlFilePath { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output file name (default = '').")]
    public string SettingsFilePath { get; set; }

    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing file.")]
    public bool ForceOverwrite { get; set; }
}