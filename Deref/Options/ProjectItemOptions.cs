using CommandLine;
using MonoUtils.UseCases;
using MonoUtils.Domain.Data;

namespace Deref.Options;

[Verb("project", HelpText = "Discover details about a project in the monolith")]
internal sealed class ProjectItemOptions : IListDisplayOptions, IListOptions
{
    [Value(0, Required = true, HelpText = "The name of the project that you want to analyze.")]
    public string? ProjectName { get; set; }

    [Option('r', "references", HelpText = "List all projects that the project references.")]
    public bool IsListReferences { get; set; }

    [Option('b', "referenced-by", HelpText = "List all projects that reference the project.")]
    public bool IsListReferencedBy { get; set; }

    [Option('w', "wix-projects", HelpText = "List all wix projects that reference the project.")]
    public bool IsListWixProjects { get; set; }

    [Option('s', "solutions", HelpText = "List all solutions that reference the project.")]
    public bool IsListSolutions { get; set; }

    [Option('d', "build-definitions", HelpText = "List all build definitions that reference the project.")]
    public bool IsListBuildDefinitions { get; set; }

    [Option('f', "find-reference", HelpText = "Search for a reference to or from this project by name.")]
    public string? SearchTerm { get; set; }

    [Option('i', "include", Default = FilterType.All, HelpText = "Filter references by their required status in our monolith")]
    public FilterType FilterBy { get; set; }

    [Option('m', "missing", Default = TodoFilterType.NoFilter, HelpText = "Filter references by missing features.")]
    public TodoFilterType TodoFilter { get; set; }

    [Option("no-tests", Default = false, HelpText = "Exclude test references from the list.")]
    public bool IsExcludeTests { get; set; }

    [Option("recursive", HelpText = "List all nested references required for the item.")]
    public bool IsRecursive { get; set; }

    [Option('c', "counts", HelpText = "List using and used by counts for each item.")]
    public bool ShowListCounts { get; set; }

    [Option('t', "todos", HelpText = "List current features needed for each item in the list.")]
    public bool ShowListTodos { get; set; }
}