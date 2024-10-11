namespace Delinq.Parsers;

internal class ScopeTracker
{
    private int _counter;
    private bool _isNewScope = true;

    public bool IsInScope(string lineOfCode)
    {
        var trimmed = lineOfCode.Trim();
        if (trimmed == "{")
        {
            _counter++;
            _isNewScope = false;
            return true;
        }

        if (trimmed is "}" or "};" or "});") 
            _counter--;

        return _isNewScope || _counter > 0;
    }
}