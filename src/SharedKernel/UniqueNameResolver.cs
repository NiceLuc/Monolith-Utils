namespace SharedKernel;

public class UniqueNameResolver
{
    public string GetUniqueName(string name, Func<string, bool> hasKey)
    {
        var offset = 0;

        var result = name;
        while (hasKey(result))
        {
            offset++;
            result = $"{name}-{offset}";
        }

        return result;
    }
}