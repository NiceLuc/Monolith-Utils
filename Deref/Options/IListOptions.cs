using CommandLine;

namespace Deref.Options;

public interface IListOptions
{
    [Option("exclude-tests", HelpText = "Exclude test items from the list.")]
    bool IsExcludeTests { get; set; }

    // note: The usage of SetName attribute field is required to ensure that only one of the options can be set (not multiple)
    // if none of them are set, will default to the user's default configuration

    /*
    [Option('f', "find", HelpText = "Find all ")]
    string? SearchTerm { get; set; }

    [Option("all", SetName = "ListAllFiles", HelpText = "List all items found in the branch.")]
    bool IsIncludeAll { get; set; }

    [Option("only-required", SetName = "ListRequiredFiles", HelpText = "Only list items that are required for our build definitions.")]
    bool IsIncludeOnlyRequired { get; set; }

    [Option("only-not-required", SetName = "ListNonRequiredFiles", HelpText = "Only list items that are NOT required for our build definitions.")]
    bool IsIncludeOnlyNonRequired { get; set; }

    */
    [Option("recursive", HelpText = "List all nested references required for the item.")]
    bool IsRecursive { get; set; }
}