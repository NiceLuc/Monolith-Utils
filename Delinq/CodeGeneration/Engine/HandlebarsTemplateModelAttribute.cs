namespace Delinq.CodeGeneration.Engine;

internal class HandlebarsTemplateModelAttribute(string key, string resourceFileName, string fileNameFormat) : Attribute
{
    public string Key { get; } = key;
    public string ResourceFileName { get; } = resourceFileName;
    public string FileNameFormat { get; } = fileNameFormat;
}