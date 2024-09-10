namespace GVLinQOptimizer.CodeGeneration.ViewModels;

public class RepositoryViewModel
{
    private readonly ContextDefinition _definition;

    public RepositoryViewModel(ContextDefinition definition)
    {
        _definition = definition;
    }

    public string Namespace => _definition.Namespace;
    public string ContextName => _definition.ContextName;

    public List<string> Methods { get; set; } = new();
}