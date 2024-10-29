using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedKernel;

public class DefinitionSerializer<T>(IFileStorage fileStorage) : IDefinitionSerializer<T>
{
    public async Task SerializeAsync(string filePath, T definition, CancellationToken cancellationToken)
    {
        var prettified = GetSerializationOptions();
        var serialized = JsonSerializer.Serialize(definition, prettified);
        await fileStorage.WriteAllTextAsync(filePath, serialized, cancellationToken);
    }

    public async Task<T> DeserializeAsync(string filePath, CancellationToken cancellationToken)
    {
        var stream = fileStorage.GetFileStreamForRead(filePath);
        var options = GetDeserializationOptions();
        var definition = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }

    protected virtual JsonSerializerOptions GetSerializationOptions()  => new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected virtual JsonSerializerOptions GetDeserializationOptions() => new();
}