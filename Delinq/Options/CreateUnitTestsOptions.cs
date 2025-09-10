using CommandLine;

namespace MonoUtils.Delinq.Options;

[Verb("tests", HelpText = "Create unit tests")]
internal class CreateUnitTestsOptions
{
    [Value(0, Required = true, HelpText = "The name of the context file you want to generate tests for (see Configs directory).")]
    public string ContextName { get; set; }

    [Option('s', HelpText = "Full path to metadata settings file.")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", HelpText = "The output directory")]
    public string OutputDirectory { get; set; }

    [Option('m', "method", HelpText = "Only generate tests for a single method.")]
    public string MethodName { get; set; }
}
