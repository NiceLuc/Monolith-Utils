namespace GVLinQOptimizer;

public class ContextDefinition
{
    public string Namespace { get; set; }
    public string ContextName { get; set; }
    public List<MethodDefinition> Methods { get; set; } = new();
    public List<TypeDefinition> Types { get; set; } = new();
}

public class MethodDefinition
{
    public string DatabaseName { get; set; }
    public string DatabaseType { get; set; }
    public string CodeName { get; set; }
    public string CodeType { get; set; }
    public bool IsList { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
}

public class ParameterDefinition
{
    public string DatabaseName { get; set; }
    public string DatabaseType { get; set; }
    public string DatabaseLength { get; set; }
    public string CodeType { get; set; }
    public string CodeName { get; set; }
    public bool IsRef { get; set; }
}

public class TypeDefinition
{
    public string ClassName { get; set; }
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class PropertyDefinition
{
    public string CodeName { get; set; }
    public string CodeType { get; set; }
}