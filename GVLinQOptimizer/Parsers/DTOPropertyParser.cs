using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class DTOPropertyParser : SettingsParser<TypeDefinition>
{
    private readonly Regex _propertyHeaderRegex = new(
        @"Mapping\.ColumnAttribute\(", 
        RegexOptions.Singleline);

    private readonly Regex _propertyRegex = new(
        "public (?<type>.+?) (?<name>.+?)$", 
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _propertyHeaderRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(TypeDefinition model, StreamReader reader)
    {
        // we don't need any information from the first line
        ReadNextLine(reader);

        var match = _propertyRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse property definition for '{CurrentLine}'");

        var propertyDefinition = new PropertyDefinition
        {
            CodeType = match.Groups["type"].Value,
            CodeName = match.Groups["name"].Value
        };

        var nullableMatch = _nullableRegex.Match(propertyDefinition.CodeType);
        if (nullableMatch.Success)
        {
            propertyDefinition.CodeType = nullableMatch.Groups["nullable_type"].Value + "?";
        }

        if (propertyDefinition.CodeType.StartsWith("System."))
            propertyDefinition.CodeType = propertyDefinition.CodeType.Replace("System.", "");

        model.Properties.Add(propertyDefinition);
    }
}