using CommandLine;

namespace Deref.Options;

[Verb("project", HelpText = "Analyze details about projects in the monolith")]
internal class ProjectOptions
{
    [Value(0, Required = false, HelpText = "The name of the branch which has the solutions you want to analyze.")]
    public string? ProjectName { get; set; }

    [Option('l', "list", SetName = "all", HelpText = "List all projects required for our build definitions.")]
    public bool IsList { get; set; }

    [Option('r', "references", SetName = "one", HelpText = "List all projects the a particular project references.")]
    public bool IsListReferences { get; set; }

    [Option('b', "referenced-by", SetName = "one", HelpText = "List all projects that reference a specific project.")]
    public bool IsListReferencedBy { get; set; }

    [Option('c', "counts", HelpText = "List using and used by counts for each project.")]
    public bool ShowListCounts { get; set; }

    [Option('t', "todos", HelpText = "List current features needed for each project.")]
    public bool ShowListTodos { get; set; }

    [Option("recursive", HelpText = "Return a unique list of all references referenced for any project.")]
    public bool IsRecursive { get; set; }
}