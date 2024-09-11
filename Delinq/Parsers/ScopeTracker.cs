namespace Delinq.Parsers;

internal class ScopeTracker
{
    private int _counter;

    public bool IsInScope(string lineOfCode)
    {
        var trimmed = lineOfCode.Trim();
        if (trimmed == "{")
        {
            _counter++;
            return true;
        }

        if (trimmed == "}") 
            _counter--;

        return _counter > 0;
    }
}