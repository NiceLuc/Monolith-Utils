namespace MonoUtils.Delinq.Parsers;

public interface IParser<in T> where T : class
{
    bool CanParse(string lineOfCode);
    void Parse(T model, StreamReader reader);
}