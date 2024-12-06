using System.Text.Json;
using System.Text.Json.Serialization;
using MonoUtils.Domain;
using MonoUtils.Infrastructure;

namespace Delinq;

public class RepositoryDefinitionSerializer(IFileStorage fileStorage) : DefinitionSerializer<RepositoryDefinition>(fileStorage)
{
    protected override JsonSerializerOptions GetDeserializationOptions() => new()
    {
        Converters =
        {
            new RepositoryMethodStatusConverter(),
            new SprocQueryTypeConverter()
        }
    };

    #region Private Classes

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

    #endregion
}