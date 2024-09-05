using System.Text.RegularExpressions;

namespace GVLinQOptimizer.Parsers;

public abstract class SettingsParser<T> : IParser<T> where T : class
{
    protected readonly Regex _nullableRegex = new Regex(
        @"Nullable\<(?<nullable_type>.+?)\>", 
        RegexOptions.Singleline);

    protected internal string CurrentLine { get; private set; } = string.Empty;

    public bool CanParse(string lineOfCode)
    {
        if (string.IsNullOrEmpty(lineOfCode.Trim()))
            return false;

        if (CanParseImpl(lineOfCode))
        {
            CurrentLine = lineOfCode;
            return true;
        }

        return false;
    }

    public void Parse(T model, StreamReader reader)
    {
        ParseImpl(model, reader);
    }

    protected void ReadNextLine(StreamReader reader)
    {
        if (reader.ReadLine() is not { } nextLine)
            throw new InvalidOperationException("Unexpected end of file");

        CurrentLine = nextLine;
    }
    protected abstract bool CanParseImpl(string lineOfCode);
    protected abstract void ParseImpl(T model, StreamReader reader);
}