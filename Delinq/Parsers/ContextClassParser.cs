using System.Text.RegularExpressions;

namespace MonoUtils.Delinq.Parsers;

internal class ContextClassParser : SettingsParser<ContextDefinition>
{
    private static readonly Regex _classRegex = new(
        @"public partial class (?<class_name>.+)DataContext \:",
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _classRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(ContextDefinition definition, StreamReader _)
    {
        var match = _classRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse class definition for '{CurrentLine}'");

        definition.ContextName = match.Groups["class_name"].Value;
    }
}