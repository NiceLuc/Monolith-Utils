namespace GVLinQOptimizer.Renderers.ViewModels;

public class RepositoryMethodViewModel
{
    public string CodeName { get; set; }
    public string DatabaseName { get; set; }
    public string DatabaseType { get; set; }
    public string CodeType { get; set; }
    public bool IsList { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; } = new();
}