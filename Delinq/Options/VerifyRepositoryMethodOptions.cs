using CommandLine;

namespace MonoUtils.Delinq.Options;

[Verb("verify", HelpText = "Verify sproc calls for a given repository file.")]
internal class VerifyRepositoryMethodOptions
{
    [Value(0, Required = true, HelpText = "The name of the context file you want to validate (see Configs directory).")]
    public string ContextName { get; set; }

    [Option('b', "branch-name", HelpText = "The name of the branch which has the repository file you are validating.")]
    public string BranchName { get; set; }

    [Option("repository-path", HelpText = "Full path to a repository implementation file (note: overrides BranchName argument).")]
    public string RepositoryFilePath { get; set; }

    [Option('c', "connection-string", HelpText = "The connection string to the database containing the stored procedures.")]
    public string ConnectionString { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output file name to list results (default = '').")]
    public string ValidationFilePath { get; set; }

    [Option('m', "method", HelpText = "Only validate a single method.")]
    public string MethodName { get; set; }

    [Option('r', "report", Default = false, HelpText = "Generate the verification report.")]
    public bool IsGenerateReport { get; set; }

    [Option('x', "open-report", Default = false, HelpText = "The report will open after it is generated")]
    public bool IsOpenReport { get; set; }
}