using System.Text.Json;

namespace GVLinQOptimizer;

public class ContextDefinitionSerializer : IContextDefinitionSerializer
{
    public async Task SerializeAsync(string filePath, ContextDefinition definition, CancellationToken cancellationToken)
    {
        var prettified = new JsonSerializerOptions {WriteIndented = true};
        var serialized = JsonSerializer.Serialize(definition, prettified);
        await File.WriteAllTextAsync(filePath, serialized, cancellationToken);
    }

    public async Task<ContextDefinition> DeserializeAsync(string filePath, CancellationToken cancellationToken)
    {
        var stream = File.OpenRead(filePath);
        var definition = await JsonSerializer.DeserializeAsync<ContextDefinition>(stream, 
            cancellationToken: cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }
}