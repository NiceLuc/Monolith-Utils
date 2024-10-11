using System.Text.RegularExpressions;

namespace Delinq.Parsers;

internal class ParametersParser : SettingsParser<MethodDefinition>
{
    private static readonly Regex _parameterRegex = new(
        @"ParameterAttribute\((\s?Name\s?\=\s?""(?<db_name>.+?)"",)?\s?DbType\s?\=\s?""(?<db_type>.+?)"".+?]\s(?<ref_token>ref\s)?(?<net_type>.+?)\s(?<net_name>.+?)[,\)]",
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
                SprocParameterName = parameterMatch.Groups["db_name"].Success
                    ? parameterMatch.Groups["db_name"].Value
                    : parameterMatch.Groups["net_name"].Value,
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

    private static void SetParameterDirection(ParameterDefinition parameter)
        => parameter.ParameterDirection = parameter.IsRef ? "Output" : "Input";

    private static void ExtractDatabaseStringLength(ParameterDefinition parameter)
    {
        var match = _charLengthRegex.Match(parameter.SqlDbType);
        if (match.Success)
        {
            parameter.SqlDbType = "NVarChar";
            parameter.DatabaseLength = match.Groups["db_length"].Value;
            return;
        }

        match = _binaryLengthRegex.Match(parameter.SqlDbType);
        if (match.Success)
        {
            parameter.ParameterType = "byte[]";
            parameter.SqlDbType = "VarBinary";
            parameter.DatabaseLength = match.Groups["db_length"].Value;
            return;
        }
    }

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

    #endregion

}