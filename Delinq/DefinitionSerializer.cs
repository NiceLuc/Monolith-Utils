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
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new RepositoryMethodStatusConverter(),
                new SprocQueryTypeConverter()
            }
        };
        var definition = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);

        if (definition == null)
            throw new InvalidOperationException($"Unable to deserialize the settings file: {filePath}");

        return definition;
    }

    private class RepositoryMethodStatusConverter : JsonConverter<RepositoryMethodStatus>
    {
        public override RepositoryMethodStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out RepositoryMethodStatus status) ? status : RepositoryMethodStatus.Unknown; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, RepositoryMethodStatus value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }

    private class SprocQueryTypeConverter : JsonConverter<SprocQueryType>
    {
        public override SprocQueryType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out SprocQueryType status) ? status : SprocQueryType.Unknown; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, SprocQueryType value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }
}