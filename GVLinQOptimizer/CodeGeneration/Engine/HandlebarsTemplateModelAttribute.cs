namespace Delinq.CodeGeneration.Engine;

internal class HandlebarsTemplateModelAttribute(string key, 
    string? resourceFileName = null, string? fileNameFormat = null) : Attribute
{
    public string Key { get; } = key;
    public string? ResourceFileName { get; } = resourceFileName;
    public string? FileNameFormat { get; } = fileNameFormat;
}