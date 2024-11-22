using CommandLine;

namespace Deref.Options;

public interface IListDisplayOptions
{
    [Option('c', "counts", HelpText = "List using and used by counts for each item.")]
    bool ShowListCounts { get; set; }

    [Option('t', "todos", HelpText = "List current features needed for each item in the list.")]
    bool ShowListTodos { get; set; }
}