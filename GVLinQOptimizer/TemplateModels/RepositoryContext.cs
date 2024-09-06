namespace GVLinQOptimizer.TemplateModels;

public class RepositoryContext
{
    private readonly ContextDefinition _definition;

    public RepositoryContext(ContextDefinition definition)
    {
        _definition = definition;
    }

    public string Namespace => _definition.Namespace;
    public string ContextName => _definition.ContextName;

    public List<string> Methods { get; set; } = new();
}

public class MethodContext
{
    public string CodeName { get; set; }
    public string DatabaseName { get; set; }
    public string DatabaseType { get; set; }
    public string CodeType { get; set; }
    public bool IsList { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; } = new();

}

public class ParameterContext
{
    public string CodeType { get; set; }
    public string CodeName { get; set; }
    public bool IsRef { get; set; }

    public string ParameterType { get; set; }
    public string ParameterName { get; set; }
    public string ParameterLength { get; set; }
    public string ParameterDirection { get; set; }
}