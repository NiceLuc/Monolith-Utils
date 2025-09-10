using CommandLine;
using MonoUtils.Domain.Data;
using MonoUtils.UseCases;

namespace MonoUtils.App.Options;

[Verb("projects", HelpText = "Find projects in the monolith")]
internal sealed class ProjectListOptions : IListDisplayOptions, IListOptions
{
    [Value(0, Required = false, HelpText = "A search term of the project that you wish to find.")]
    public string? SearchTerm { get; set; }

    [Option('i', "include", Required = false, Default = FilterType.All, HelpText = "Filter results by their required status in our monolith")]
    public FilterType FilterBy { get; set; }

    [Option("missing", Required = false, Default = TodoFilterType.NoFilter, HelpText = "Filter results by missing features.")]
    public TodoFilterType TodoFilter { get; set; }

    [Option("no-tests", Default = false, HelpText = "Exclude test items from the list.")]
    public bool IsExcludeTests { get; set; }

    [Option('c', "counts", HelpText = "List using and used by counts for each item.")]
    public bool ShowListCounts { get; set; }

    [Option('t', "todos", HelpText = "List current features needed for each item in the list.")]
    public bool ShowListTodos { get; set; }
}