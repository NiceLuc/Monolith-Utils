namespace GVLinQOptimizer.Renderers;

internal class HandlebarsTemplateModelAttribute : Attribute
{
    public string ResourceFileName { get; }
    public string? FileNameFormat { get; }

    public HandlebarsTemplateModelAttribute(string resourceFileName, string? fileNameFormat = null)
    {
        ResourceFileName = resourceFileName;
        FileNameFormat = fileNameFormat;
    }
}