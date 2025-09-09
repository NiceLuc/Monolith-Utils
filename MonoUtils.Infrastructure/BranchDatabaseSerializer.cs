using System.Text.Json;
using System.Text.Json.Serialization;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseSerializer(IFileStorage fileStorage) : DefinitionSerializer<BranchDatabase>(fileStorage)
{
    protected override JsonSerializerOptions GetDeserializationOptions() => new()
    {
        Converters =
        {
            new ProjectTypeValueConverter(),
            new ErrorSeverityValueConverter(),
            new RecordTypeValueConverter()
        }
    };

    #region Private Classes

    private class ProjectTypeValueConverter : JsonConverter<ProjectType>
    {
        public override ProjectType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out ProjectType type) ? type : ProjectType.Unknown; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, ProjectType value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }

    private class ErrorSeverityValueConverter : JsonConverter<ErrorSeverity>
    {
        public override ErrorSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out ErrorSeverity severity) ? severity : ErrorSeverity.Info; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, ErrorSeverity value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }

    private class RecordTypeValueConverter : JsonConverter<RecordType>
    {
        public override RecordType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out RecordType type) ? type : RecordType.Unknown; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, RecordType value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }

    #endregion
}