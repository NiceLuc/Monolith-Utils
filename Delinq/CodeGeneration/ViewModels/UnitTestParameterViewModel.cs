namespace Delinq.CodeGeneration.ViewModels;

public class UnitTestParameterViewModel
{
    public string ParameterType { get; set; }
    public string ParameterName { get; set; }
    public string FakeValue { get; set; }
    public bool IsNullable { get; set; }

    public bool IsRef { get; set; }
    public bool IsInputParameter { get; set; }
    public string InitialValue { get; set; }
}

public class UnitTestPropertyViewModel
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public bool IsString => PropertyType == "string";

    public bool IsNumber => PropertyType.ToLower().Replace("?", "") switch
    {
        "int" => true,
        "long" => true,
        "decimal" => true,
        "float" => true,
        _ => false
    };

    public string FakeValue { get; set; }
}