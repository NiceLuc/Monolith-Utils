using CommandLine;

namespace Delinq.Options;

[Verb("dtos", HelpText = "Extract DTOs from settings file.")]
internal class ExtractDTOOptions
{
    [Value(0, HelpText = "Full path to the settings file")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output directory or file path.")]
    public string OutputDirectory { get; set; }
}
