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
}