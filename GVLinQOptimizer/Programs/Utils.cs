using System.Text.Json;

namespace GVLinQOptimizer.Programs;

public static class Utils
{
    public static async Task<ContextDefinition> LoadSettingsFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var stream = File.OpenRead(filePath);
        var definition = await JsonSerializer.DeserializeAsync<ContextDefinition>(stream, 
            cancellationToken: cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }
    public static async Task<string> GetResourceAsync(string fileName, CancellationToken cancellationToken)
    {
        var assembly = typeof(Program).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName));
        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
            throw new InvalidOperationException($"Unable to locate resource: {fileName}");

        using var sr = new StreamReader(resourceStream);
        return await sr.ReadToEndAsync(cancellationToken);
    }
}