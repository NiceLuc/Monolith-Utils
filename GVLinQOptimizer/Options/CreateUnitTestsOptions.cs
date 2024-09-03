using CommandLine;

namespace GVLinQOptimizer.Options;

[Verb("tests", HelpText = "Create unit tests")]
internal class CreateUnitTestsOptions
{
    [Option("designer", Required = true, HelpText = "The designer file")]
    public string DesignerFilePath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output directory")]
    public string OutputDirectory { get; set; }
}
