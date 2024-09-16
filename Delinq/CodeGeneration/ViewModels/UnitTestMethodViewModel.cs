namespace Delinq.CodeGeneration.ViewModels;

public class UnitTestMethodViewModel
{
    public string MethodName { get; set; }
    public string SprocName { get; set; }
    public string DatabaseType { get; set; }
    public string ReturnType { get; set; }
    public bool IsList { get; set; }

    public List<UnitTestParameterViewModel> Parameters { get; set; } = new();
    public IEnumerable<UnitTestParameterViewModel> RefParameters => Parameters.Where(p => p.IsRef);

    public List<UnitTestPropertyViewModel> Properties { get; set; }

    public bool HasOutputParameters => Parameters.Any(p => p.IsRef);
    public IList<UnitTestParameterViewModel> OutputParameters => Parameters.Where(p => p.IsRef).ToList();

    public bool HasReturnValue => ReturnValueParameter != null;
    public UnitTestParameterViewModel? ReturnValueParameter { get; set; }
}