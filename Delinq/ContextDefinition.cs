namespace Delinq;

public class ContextDefinition
{
    public string Namespace { get; set; }
    public string ContextName { get; set; }
    public List<MethodDefinition> RepositoryMethods { get; set; } = new();
    public List<DTOClassDefinition> DTOModels { get; set; } = new();
}

public class MethodDefinition
{
    public string DatabaseName { get; set; }
    public string DatabaseType { get; set; }
    public string MethodName { get; set; }
    public string ReturnType { get; set; }
    public bool IsList { get; set; }
    public bool HasReturnParameter { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
}

public class ParameterDefinition
{
    public string SprocParameterName { get; set; }
    public string SqlDbType { get; set; }
    public string DatabaseLength { get; set; }
    public string ParameterType { get; set; }
    public string ParameterName { get; set; }
    public bool IsRef { get; set; }
    public string ParameterDirection { get; set; }
}

public class DTOClassDefinition
{
    public string ClassName { get; set; }
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class PropertyDefinition
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
}