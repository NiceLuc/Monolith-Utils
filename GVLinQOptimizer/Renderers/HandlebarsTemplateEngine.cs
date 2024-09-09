using Mustache;

namespace GVLinQOptimizer.Renderers;

internal class HandlebarsTemplateEngine : ITemplateEngine
{
    private readonly IDictionary<string, string> _resourceMap;
    private readonly FormatCompiler _compiler;

    public HandlebarsTemplateEngine(FormatCompiler compiler)
    {
        _compiler = compiler;
        _resourceMap = new Dictionary<string, string>();
    }

    public async Task<string> ProcessAsync(string resourceFileName, object data, CancellationToken cancellationToken)
    {
        ValidateParameters(resourceFileName, data);

        if (!_resourceMap.TryGetValue(resourceFileName, out var template))
        {
            template = await GetResourceAsync(resourceFileName, cancellationToken);
            _resourceMap.Add(resourceFileName, template);
        }

        // process the template using the data parameter
        var generator = _compiler.Compile(template);
        return generator.Render(data);
    }

    #region Private Methods

    private static void ValidateParameters(string resourceFileName, object data)
    {
        if (string.IsNullOrEmpty(resourceFileName))
            throw new ArgumentException("Value cannot be null or empty.", nameof(resourceFileName));
        if (data == null) throw new ArgumentNullException(nameof(data));
    }

    private static async Task<string> GetResourceAsync(string fileName, CancellationToken cancellationToken)
    {
        var assembly = typeof(Program).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName));
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
            throw new InvalidOperationException($"Unable to locate resource: {fileName}");

        using var sr = new StreamReader(resourceStream);
        return await sr.ReadToEndAsync(cancellationToken);
    }

    #endregion
}