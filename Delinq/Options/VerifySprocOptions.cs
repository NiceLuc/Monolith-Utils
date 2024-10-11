using CommandLine;

namespace Delinq.Options;

[Verb("verify", HelpText = "Verify sproc calls for a given repository file.")]
internal class VerifySprocOptions
{
    [Value(0, Required = true, HelpText = "Full path to a legacy DBML file.")]
    public string RepositoryFilePath { get; set; }

    [Option('c', "connection-string", Required = true, HelpText = "The connection string to the database containing the stored procedures.")]
    public string ConnectionString { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output file name to list results (default = '').")]
    public string ValidationFilePath { get; set; }

    [Option('m', "method", HelpText = "Only validate a single method.")]
    public string MethodName { get; set; }
}