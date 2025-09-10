using CommandLine;

namespace MonoUtils.Delinq.Options;

[Verb("init", HelpText = "Initialize settings file from designer file.")]
internal class InitializeOptions
{
    [Value(0, Required = true, HelpText = "The name of the context file you want to parse (see Configs directory).")]
    public string ContextName { get; set; }

    [Option('b', "branch-name", HelpText = "The name of the branch which has the LINQ files you are parsing.")]
    public string BranchName { get; set; }

    [Option("dbml", HelpText = "Full path to a LINQ DBML file.")]
    public string DbmlFilePath { get; set; }

    [Option("designer", HelpText = "Full path to a LINQ designer file.")]
    public string DesignerFilePath { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output file name of the meta data file (default = '').")]
    public string SettingsFilePath { get; set; }

    [Option('f', "force", Default = false, HelpText = "Set this to true to overwrite existing file.")]
    public bool ForceOverwrite { get; set; }
}