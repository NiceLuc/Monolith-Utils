using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class ParametersParser : SettingsParser<MethodDefinition>
{
    private static readonly Regex _parameterRegex = new(
        @"ParameterAttribute\(Name\=""(?<db_name>.+?)"",\sDbType\=""(?<db_type>.+?)"".+?]\s(?<ref_token>ref\s)?(?<net_type>.+?)\s(?<net_name>.+?)[,\)]",
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => true;

    protected override void ParseImpl(MethodDefinition method, StreamReader reader)
    {
        // extract all parameters from the method line
        var parameterMatches = _parameterRegex.Matches(CurrentLine);
        foreach (Match parameterMatch in parameterMatches)
        {
            var parameter = new ParameterDefinition
            {
                SprocParameterName = parameterMatch.Groups["db_name"].Value,
                SqlDbType = parameterMatch.Groups["db_type"].Value,
                ParameterName = parameterMatch.Groups["net_name"].Value,
                ParameterType = parameterMatch.Groups["net_type"].Value,
                IsRef = parameterMatch.Groups["ref_token"].Success
            };

            SetParameterDirection(parameter);
            ExtractDatabaseStringLength(parameter);
            CleanUpSystemTypes(parameter);

            method.Parameters.Add(parameter);
        }
    }

    #region Private Methods

    private void CleanUpSystemTypes(ParameterDefinition parameter)
    {
        var nullableMatch = _nullableRegex.Match(parameter.ParameterType);
        if (nullableMatch.Success)
        {
            parameter.ParameterType = nullableMatch.Groups["nullable_type"].Value + "?";
        }

        if (parameter.ParameterType.StartsWith("System."))
            parameter.ParameterType = parameter.ParameterType.Replace("System.", "");
    }

    private static void ExtractDatabaseStringLength(ParameterDefinition parameter)
    {
        var stringMatch = _charLengthRegex.Match(parameter.SqlDbType);
        if (stringMatch.Success)
        {
            parameter.SqlDbType = "NVarChar";
            parameter.DatabaseLength = stringMatch.Groups["db_length"].Value;
        }
    }

    private static void SetParameterDirection(ParameterDefinition parameter)
    {
        if (!parameter.IsRef)
        {
            parameter.ParameterDirection = "Input";
            return;
        }

        if (parameter.ParameterName == "rowCount")
        {
            parameter.ParameterDirection = "ReturnValue";
            return;
        }

        parameter.ParameterDirection = "Output";
    }

    #endregion

}