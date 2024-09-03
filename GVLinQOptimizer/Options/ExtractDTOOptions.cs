using CommandLine;

namespace GVLinQOptimizer.Options;

[Verb("dtos", HelpText = "Extract DTOs from settings file.")]
internal class ExtractDTOOptions
{
    [Value(0, HelpText = "The designer file")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output directory or file path.")]
    public string OutputFileOrDirectory { get; set; }
}
