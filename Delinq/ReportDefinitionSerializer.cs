using System.Text.Json;

namespace Delinq;

public class RepositoryDefinitionSerializer(IFileStorage fileStorage) : IContextDefinitionSerializer<RepositoryDefinition>
{
    public async Task SerializeAsync(string filePath, RepositoryDefinition definition, CancellationToken cancellationToken)
    {
        var prettified = new JsonSerializerOptions {WriteIndented = true};
        var serialized = JsonSerializer.Serialize(definition, prettified);
        await fileStorage.WriteAllTextAsync(filePath, serialized, cancellationToken);
    }

    public async Task<RepositoryDefinition> DeserializeAsync(string filePath, CancellationToken cancellationToken)
    {
        var stream = File.OpenRead(filePath);
        var definition = await JsonSerializer.DeserializeAsync<RepositoryDefinition>(stream, 
            cancellationToken: cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }
}