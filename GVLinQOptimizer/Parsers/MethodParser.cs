using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

internal class MethodParser : SettingsParser<ContextDefinition>
{
    private static readonly Regex _sprocRegex = new Regex(
        @"FunctionAttribute\(Name\=""(?<sproc_name>.+)""", 
        RegexOptions.Singleline);

    private static readonly Regex _methodRegex = new Regex(
        @"public (ISingleResult\<(?<return_type>.+?)\>|(?<return_type>.+?))\s(?<method_name>.+?)\(", 
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
            throw new InvalidOperationException($"Unable to parse method definition for '{CurrentLine}'");

        method.CodeType = match.Groups["return_type"].Value;
        method.CodeName = match.Groups["method_name"].Value;

        if (CurrentLine.Contains("ISingleResult"))
        {
            method.DatabaseType = "Query";

            // todo: determine T vs. IEnumerable<T> for IsList
            method.IsList = true;
        }
        else
        {
            method.DatabaseType = "NonQuery";
        }

        // extract all parameters from the method line
        var parameterParser = new ParametersParser(this);
        parameterParser.Parse(method, reader);

        // add the fully hydrated method to the definition
        definition.Methods.Add(method);
    }
}