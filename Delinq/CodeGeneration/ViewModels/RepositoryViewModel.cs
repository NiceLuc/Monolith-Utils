﻿namespace Delinq.CodeGeneration.ViewModels;

public class RepositoryViewModel(ContextDefinition definition)
{
    public string Namespace => definition.Namespace;
    public string ContextName => definition.ContextName;

    public List<string> Methods { get; set; } = new();
}