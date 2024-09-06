using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class DTOClassParser : SettingsParser<ContextDefinition>
{
    private static readonly Regex _classRegex = new(
        "public partial class (?<class_name>.+)", 
        RegexOptions.Singleline);

    private readonly IParser<TypeDefinition> _propertyParser;
    private readonly ScopeTracker _scopeTracker;

    public DTOClassParser(IParser<TypeDefinition> propertyParser, ScopeTracker scopeTracker)
    {
        _propertyParser = propertyParser;
        _scopeTracker = scopeTracker;
    }

    protected override bool CanParseImpl(string lineOfCode) => _classRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(ContextDefinition definition, StreamReader reader)
    {
        var match = _classRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse DTO class definition for '{CurrentLine}'");

        var typeDefinition = new TypeDefinition {ClassName = match.Groups["class_name"].Value};

        // extract all properties from the class
        ReadNextLine(reader);

        // here, we must capture the spacing of the '{' character
        // then we read all lines until the closing bracket is found
        while (_scopeTracker.IsInScope(CurrentLine))
        {
            if (_propertyParser.CanParse(CurrentLine)) 
                _propertyParser.Parse(typeDefinition, reader);

            ReadNextLine(reader);
        }

        definition.Types.Add(typeDefinition);
    }

}