using CommandLine;

namespace MonoUtils.Delinq.Options;

[Verb("repo", HelpText = "Creates various files required for a repository based approach from an existing designer file.")]
internal class CreateRepositoryOptions
{
    [Value(0, Required = true, HelpText = "The name of the context file you want to generate repository classes for (see Configs directory).")]
    public string ContextName { get; set; }

    [Option('s', HelpText = "Full path to metadata settings file.")]
    public string SettingsFilePath { get; set; }

    [Option('o', "output", HelpText = "The output directory of the repository files.")]
    public string OutputDirectory { get; set; }

    [Option('m', "method", HelpText = "Only generate code for a single method.")]
    public string MethodName { get; set; }
}
