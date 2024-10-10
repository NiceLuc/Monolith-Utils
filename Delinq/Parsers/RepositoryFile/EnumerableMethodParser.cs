using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Delinq.Parsers.RepositoryFile;

internal class EnumerableMethodParser : SettingsParser<RepositoryDefinition>
{
    private static readonly Regex _methodRegex = new(
        @"public IEnumerable\<(?<return_type>.+?)\>\s(?<method_name>.+?)\(",
        RegexOptions.Singleline);

    protected override bool CanParseImpl(string lineOfCode) => _methodRegex.IsMatch(lineOfCode);

    protected override void ParseImpl(RepositoryDefinition model, StreamReader reader)
    {
        var match = _methodRegex.Match(CurrentLine);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse class definition for '{CurrentLine}'");

        var result = new EnumerableMethod
        {
            // todo!
        };

        var code = ExtractMethodCode(reader);

        Console.Write(code);
    }

    private string ExtractMethodCode(StreamReader reader)
    {
        // this will hold the entire contents of the method (definition and logic)
        var builder = new StringBuilder();
        var scopeTracker = new ScopeTracker();

        // capture the method definition (include return type and parameters) as a string
        while (true)
        {
            builder.AppendLine(CurrentLine);

            if (!scopeTracker.IsInScope(CurrentLine))
                break;

            ReadNextLine(reader);
        }

        var code = builder.ToString();
        return code;
    }
}