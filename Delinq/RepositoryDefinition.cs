namespace Delinq;

public class ConnectionStrings
{
    public string InCode { get; set; }
}

public class RepositoryDefinition
{
    public string FilePath { get; set; }
    public List<EnumerableMethod> EnumerableMethods { get; set; } = new();
}

public class EnumerableMethod
{
    public bool IsOk { get; set; }

    public string MethodName { get; set; }
    public string MethodReturnType { get; set; }
    public List<ParameterDefinition> MethodParameters { get; set; } = new();

    public string SprocName { get; set; }
    public string SprocIsNonQuery { get; set; }
    public List<ParameterDefinition> SprocParameters { get; set; } = new();
}
