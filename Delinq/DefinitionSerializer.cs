using System.Text.Json;
using System.Text.Json.Serialization;

namespace Delinq;

public class DefinitionSerializer<T>(IFileStorage fileStorage) : IDefinitionSerializer<T>
{
    public async Task SerializeAsync(string filePath, T definition, CancellationToken cancellationToken)
    {
        var prettified = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        var serialized = JsonSerializer.Serialize(definition, prettified);
        await fileStorage.WriteAllTextAsync(filePath, serialized, cancellationToken);
    }

    public async Task<T> DeserializeAsync(string filePath, CancellationToken cancellationToken)
    {
        var stream = File.OpenRead(filePath);
        var definition = await JsonSerializer.DeserializeAsync<T>(stream, 
            cancellationToken: cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }
}