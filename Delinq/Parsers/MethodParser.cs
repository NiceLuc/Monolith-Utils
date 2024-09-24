using System.Text.RegularExpressions;

namespace Delinq.Parsers;

internal class MethodParser(IParser<MethodDefinition> parameterParser) : SettingsParser<ContextDefinition>
{
    private static readonly Regex _sprocRegex = new(
        @"FunctionAttribute\(\s?Name\s?\=\s?""(?<sproc_name>.+)""", 
        RegexOptions.Singleline);

    private static readonly Regex _methodRegex = new(
        @"(public|protected internal) (ISingleResult\<(?<return_type>.+?)\>|(?<return_type>.+?))\s(?<method_name>.+?)\(", 
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _sprocRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(ContextDefinition definition, StreamReader reader)
    {
        var match = _sprocRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse stored procedure name for method: '{CurrentLine}'");

        var method = new MethodDefinition {DatabaseName = match.Groups["sproc_name"].Value};

        ReadNextLine(reader);

        match = _methodRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse method definition for '{method.DatabaseName}'");

        method.ReturnType = match.Groups["return_type"].Value;
        method.MethodName = match.Groups["method_name"].Value;

        if (CurrentLine.Contains("ISingleResult"))
        {
            method.DatabaseType = "Query";

            // todo: determine T vs. IEnumerable<T> for IsList
            method.IsList = true;
        }
        else
        {
            method.DatabaseType = "NonQuery";

            // todo: determine void vs. return value for non-query calls
            // method.IsVoid = false;
        }

        // extract all parameters from the method line
        if (parameterParser.CanParse(CurrentLine))
            parameterParser.Parse(method, reader);

        // add the fully hydrated method to the definition
        definition.RepositoryMethods.Add(method);
    }
}