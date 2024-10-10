using System.Text.RegularExpressions;

namespace Delinq.Parsers.DesignerFile;

internal class DTOClassParser(IParser<DTOClassDefinition> propertyParser, ScopeTracker scopeTracker)
    : SettingsParser<ContextDefinition>
{
    private static readonly Regex _classRegex = new(
        "public partial class (?<class_name>.+)",
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _classRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(ContextDefinition definition, StreamReader reader)
    {
        var match = _classRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse DTO class definition for '{CurrentLine}'");

        var typeDefinition = new DTOClassDefinition { ClassName = match.Groups["class_name"].Value };

        // extract all properties from the class
        ReadNextLine(reader);

        // here, we must capture the spacing of the '{' character
        // then we read all lines until the closing bracket is found
        while (scopeTracker.IsInScope(CurrentLine))
        {
            if (propertyParser.CanParse(CurrentLine))
                propertyParser.Parse(typeDefinition, reader);

            ReadNextLine(reader);
        }

        definition.DTOModels.Add(typeDefinition);
    }

}