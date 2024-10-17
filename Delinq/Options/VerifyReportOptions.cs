using CommandLine;

namespace Delinq.Options;

[Verb("report", HelpText = "Generate a csv report of the repository validation scan.")]
internal class VerifyReportOptions
{
    [Value(0, Required = true, HelpText = "The name of the context file you want to validate (see Configs directory).")]
    public string ContextName { get; set; }

    [Option('f', HelpText = "Full path to a validation json file (overrides ContextName).")]
    public string ValidationFilePath { get; set; }

    [Option('r', "report", HelpText = "File name or path where to save the results in xlsx format.")]
    public string ReportFilePath { get; set; }

    [Option('x', "open-report", Default = false, HelpText = "The report will open after it is generated")]
    public bool IsOpenReport { get; set; }
}