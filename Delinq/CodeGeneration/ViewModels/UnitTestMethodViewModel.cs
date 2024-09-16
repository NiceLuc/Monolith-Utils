namespace Delinq.CodeGeneration.ViewModels;

public class UnitTestMethodViewModel
{
    public string MethodName { get; set; }
    public string SprocName { get; set; }
    public string DatabaseType { get; set; }
    public string ReturnType { get; set; }
    public bool IsList { get; set; }

    public List<UnitTestParameterViewModel> Parameters { get; set; } = new();
    public IEnumerable<UnitTestParameterViewModel> RefParameters => Parameters.Where(p => p.ShouldCaptureResult);

    public List<PropertyDefinition> Properties { get; set; }

    public List<UnitTestParameterViewModel> SprocParameters { get; set; } = new();

    public bool HasOutputParameters => SprocParameters.Any(p => p.ShouldCaptureResult);
    public IList<UnitTestParameterViewModel> OutputParameters 
        => SprocParameters.Where(p => p.ShouldCaptureResult).ToList();

    public bool HasReturnValue => ReturnValueParameter != null;
    public UnitTestParameterViewModel? ReturnValueParameter { get; set; }
}