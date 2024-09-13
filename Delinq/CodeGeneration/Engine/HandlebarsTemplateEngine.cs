using Mustache;

namespace Delinq.CodeGeneration.Engine;

internal class HandlebarsTemplateEngine(FormatCompiler compiler, ITemplateProvider provider) : ITemplateEngine
{
    public async Task<string> ProcessAsync(string resourceFileName, object data, CancellationToken cancellationToken)
    {
        ValidateParameters(resourceFileName, data);

        var template = await provider.GetTemplateAsync(resourceFileName, cancellationToken);

        return ProcessTemplate(template, data);
    }

    public string ProcessTemplate(string templateString, object data)
    {
        var generator = compiler.Compile(templateString);
        return generator.Render(data);
    }

    #region Private Methods

    private static void ValidateParameters(string resourceFileName, object data)
    {
        if (string.IsNullOrEmpty(resourceFileName))
            throw new ArgumentException("Value cannot be null or empty.", nameof(resourceFileName));
        if (data == null) throw new ArgumentNullException(nameof(data));
    }

    #endregion
}