using System.Text.RegularExpressions;

namespace Delinq.Parsers;

public abstract class SettingsParser<T> : IParser<T> where T : class
{
    protected static readonly Regex _charLengthRegex = new(
        @"nvarchar\((?<db_length>.+?)\)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    protected static readonly Regex _binaryLengthRegex = new(
        @"varbinary\((?<db_length>.+?)\)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase);

    protected readonly Regex _nullableRegex = new(
        @"Nullable\<(?<nullable_type>.+?)\>",
        RegexOptions.Singleline);

    protected string CurrentLine { get; private set; } = string.Empty;

    public bool CanParse(string lineOfCode)
    {
        if (string.IsNullOrEmpty(lineOfCode.Trim()))
            return false;

        if (!CanParseImpl(lineOfCode))
            return false;

        CurrentLine = lineOfCode;
        return true;
    }

    public void Parse(T model, StreamReader reader)
    {
        ParseImpl(model, reader);
    }

    protected abstract bool CanParseImpl(string lineOfCode);

    protected abstract void ParseImpl(T model, StreamReader reader);

    protected void ReadNextLine(StreamReader reader)
    {
        if (reader.ReadLine() is not { } nextLine)
            throw new InvalidOperationException("Unexpected end of file");

        CurrentLine = nextLine;
    }
}