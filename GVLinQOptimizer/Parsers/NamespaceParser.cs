using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class NamespaceParser : SettingsParser<ContextDefinition>
{
    private static readonly Regex _namespaceRegex = new(
        @"^namespace (?<namespace>.+)$", 
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _namespaceRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(ContextDefinition definition, StreamReader _)
    {
        var match = _namespaceRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse namespace for '{CurrentLine}'");

        definition.Namespace = match.Groups["namespace"].Value;
    }
}