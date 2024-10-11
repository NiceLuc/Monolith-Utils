using System.Text.RegularExpressions;

namespace Delinq.Parsers;

internal class DTOPropertyParser : SettingsParser<DTOClassDefinition>
{
    private readonly Regex _propertyHeaderRegex = new(
        @"Mapping\.ColumnAttribute\(",
        RegexOptions.Singleline);

    private readonly Regex _propertyRegex = new(
        "public (?<type>.+?) (?<name>.+?)$",
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _propertyHeaderRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(DTOClassDefinition model, StreamReader reader)
    {
        // we don't need any information from the first line
        ReadNextLine(reader);

        var match = _propertyRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse property definition for '{CurrentLine}'");

        var propertyDefinition = new PropertyDefinition
        {
            PropertyType = match.Groups["type"].Value,
            PropertyName = match.Groups["name"].Value
        };

        if (propertyDefinition.PropertyType.StartsWith("System."))
            propertyDefinition.PropertyType = propertyDefinition.PropertyType.Replace("System.", "");

        var nullableMatch = _nullableRegex.Match(propertyDefinition.PropertyType);
        if (nullableMatch.Success)
        {
            propertyDefinition.PropertyType = nullableMatch.Groups["nullable_type"].Value + "?";
        }

        if (propertyDefinition.PropertyType.Contains("Linq.Binary"))
        {
            propertyDefinition.PropertyType = "byte[]";
        }

        model.Properties.Add(propertyDefinition);
    }
}