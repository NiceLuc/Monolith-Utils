using CommandLine;

namespace GVLinQOptimizer.Options;

[Verb("repo", HelpText = "Create a standard repository class from a designer file.")]
internal class CreateRepositoryOptions
{
    [Value(0, Required = true, HelpText = "Full path to settings file.")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output directory.")]
    public string OutputDirectory { get; set; }
}
