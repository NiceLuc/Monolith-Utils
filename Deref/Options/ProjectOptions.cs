using CommandLine;

namespace Deref.Options;

[Verb("project", HelpText = "Analyze details about projects in the monolith")]
internal class ProjectOptions : IListDisplayOptions, IListOptions
{
    [Value(0, Required = false, HelpText = "The name of the project that you want to analyze. (Note: Used as a 'contains' when using --list options)")]
    public string? ProjectName { get; set; }

    [Option('l', "list", SetName = "all", HelpText = "List all projects required for our build definitions.")]
    public bool IsList { get; set; }

    [Option('r', "references", SetName = "one", HelpText = "List all projects the a particular project references.")]
    public bool IsListReferences { get; set; }

    [Option('b', "referenced-by", SetName = "one", HelpText = "List all projects that reference a specific project.")]
    public bool IsListReferencedBy { get; set; }

    [Option('w', "wix-projects", SetName = "one", HelpText = "List all wix projects that reference a specific project.")]
    public bool IsListWixProjects { get; set; }

    [Option('s', "solutions", SetName = "one", HelpText = "List all solutions that reference a specific project.")]
    public bool IsListSolutions { get; set; }

    [Option('d', "build-definitions", SetName = "one", HelpText = "List all build definitions that reference a specific project.")]
    public bool IsListBuildDefinitions { get; set; }

    #region IListOptions implementation

    // list options (applies to dependency list results as well as list results)
    public string? SearchTerm { get; set; }
    public bool IsExcludeTests { get; set; }
    public bool IsIncludeAll { get; set; }
    public bool IsIncludeOnlyRequired { get; set; }
    public bool IsIncludeOnlyNonRequired { get; set; }
    public bool IsRecursive { get; set; }

    #endregion

    #region IListDisplayOptions implementation

    public bool ShowListCounts { get; set; }
    public bool ShowListTodos { get; set; }

    #endregion
}