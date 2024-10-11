using CommandLine;

namespace Delinq.Options;

[Verb("report", HelpText = "Generate a csv report of the repository validation scan.")]
internal class VerifyReportOptions
{
    [Value(0, Required = true, HelpText = "Full path to a validation json file.")]
    public string ValidationFilePath { get; set; }

    [Option('r', "report", HelpText = "Generate csv report of results")]
    public string ReportName { get; set; }
}