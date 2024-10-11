namespace Delinq.Parsers;

internal class ScopeTracker(string startToken = "{", string endToken = "}")
{
    private int _counter;
    private bool _isNewScope = true;

    public bool IsInScope(string lineOfCode)
    {
        var trimmed = lineOfCode.Trim();
        if (trimmed.Equals(startToken, StringComparison.OrdinalIgnoreCase))
        {
            _counter++;
            _isNewScope = false;
            return true;
        }

        if (trimmed.Equals(endToken, StringComparison.OrdinalIgnoreCase)) 
            _counter--;

        return _isNewScope || _counter > 0;
    }
}