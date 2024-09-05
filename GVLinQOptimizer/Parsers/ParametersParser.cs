using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class ParametersParser : SettingsParser<MethodDefinition>
{
    private static readonly Regex _parameterRegex = new Regex(
        @"ParameterAttribute\(Name\=""(?<db_name>.+?)"",\sDbType\=""(?<db_type>.+?)"".+?]\s(?<ref_token>ref\s)?(?<net_type>.+?)\s(?<net_name>.+?)[,\)]",
        RegexOptions.Singleline);

    private static readonly Regex _charLengthRegex = new Regex(
        @"nvarchar\((?<db_length>.+?)\)", 
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private readonly MethodParser _parent;

    public ParametersParser(MethodParser parent)
    {
        _parent = parent;
    }

    protected override bool CanParseImpl(string lineOfCode) => true;

    protected override void ParseImpl(MethodDefinition method, StreamReader reader)
    {
        // extract all parameters from the method line
        var parameterMatches = _parameterRegex.Matches(_parent.CurrentLine);
        foreach (Match parameterMatch in parameterMatches)
        {
            var parameterDefinition = new ParameterDefinition
            {
                DatabaseName = parameterMatch.Groups["db_name"].Value,
                DatabaseType = parameterMatch.Groups["db_type"].Value,
                CodeName = parameterMatch.Groups["net_name"].Value,
                CodeType = parameterMatch.Groups["net_type"].Value,
                IsRef = parameterMatch.Groups["ref_token"].Success
            };

            var stringMatch = _charLengthRegex.Match(parameterDefinition.DatabaseType);
            if (stringMatch.Success)
            {
                parameterDefinition.DatabaseType = "nvarchar";
                parameterDefinition.DatabaseLength = stringMatch.Groups["db_length"].Value;
            }

            var nullableMatch = _nullableRegex.Match(parameterDefinition.CodeType);
            if (nullableMatch.Success)
            {
                parameterDefinition.CodeType = nullableMatch.Groups["nullable_type"].Value + "?";
            }

            method.Parameters.Add(parameterDefinition);
        }
    }
}