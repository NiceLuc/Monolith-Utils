namespace Delinq.CodeGeneration.ViewModels;

public class UnitTestParameterViewModel
{
    public string ParameterType { get; set; }
    public string ParameterName { get; set; }
    public string FakeValue { get; set; }
    public bool IsNullable { get; set; }

    public bool ShouldCaptureResult { get; set; }
    public bool IsInputParameter { get; set; }
    public string InitialValue { get; set; }
}