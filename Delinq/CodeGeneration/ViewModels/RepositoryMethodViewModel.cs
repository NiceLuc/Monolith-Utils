namespace Delinq.CodeGeneration.ViewModels;

public class RepositoryMethodViewModel
{
    public string MethodName { get; set; }
    public string SprocName { get; set; }
    public string DatabaseType { get; set; }
    public string ReturnType { get; set; }
    public bool IsList { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; }

    public List<RepositoryParameterViewModel> SprocParameters { get; set; } = new();
    public bool HasOutputParameters => SprocParameters.Any(p => p.ShouldCaptureResult);

    public bool HasReturnValue => ReturnValueParameter != null;
    public RepositoryParameterViewModel? ReturnValueParameter { get; set; }
}