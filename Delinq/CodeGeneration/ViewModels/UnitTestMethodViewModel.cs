﻿namespace Delinq.CodeGeneration.ViewModels;

public class UnitTestMethodViewModel
{
    public string MethodName { get; set; }
    public string SprocName { get; set; }
    public string DatabaseType { get; set; }
    public string ReturnType { get; set; }
    public bool IsList { get; set; }

    public List<ParameterDefinition> Parameters { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; }

    public List<UnitTestParameterViewModel> SprocParameters { get; set; } = new();

    public IList<UnitTestParameterViewModel> OutputParameters => SprocParameters.Where(p => p.ShouldCaptureResult).ToList();
}