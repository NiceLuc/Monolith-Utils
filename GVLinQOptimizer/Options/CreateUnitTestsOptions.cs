using CommandLine;

namespace GVLinQOptimizer.Options;

[Verb("tests", HelpText = "Create unit tests")]
internal class CreateUnitTestsOptions
{
    [Value(0, Required = true, HelpText = "Full path to settings file.")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output directory")]
    public string OutputDirectory { get; set; }

    [Option('m', "method", HelpText = "Only generate test code for a single method.")]
    public string MethodName { get; set; }
}
