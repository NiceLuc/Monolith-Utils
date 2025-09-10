namespace MonoUtils.Delinq.CodeGeneration.ViewModels;

public class RepositoryParameterViewModel
{
    public string MethodParameterType { get; set; }
    public string MethodParameterName { get; set; }

    public string SprocParameterType { get; set; }
    public string SprocParameterName { get; set; }
    public string SprocParameterLength { get; set; }
    public string SprocParameterDirection { get; set; }

    public bool ShouldCaptureResult { get; set; }
    public bool IsInputParameter { get; set; }
    public bool HasStringLength { get; set; }
}