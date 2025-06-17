using System.Text.Json;
using System.Text.Json.Serialization;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseSerializer(IFileStorage fileStorage) : DefinitionSerializer<BranchDatabase>(fileStorage)
{
    protected override JsonSerializerOptions GetDeserializationOptions() => new()
    {
        Converters = {new ProjectTypeValueConverter()}
    };

    #region Private Classes

    private class ProjectTypeValueConverter : JsonConverter<ProjectType>
    {
        public override ProjectType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => Enum.TryParse(reader.GetString(), out ProjectType type) ? type : ProjectType.Unknown; // Handle unknown values

        public override void Write(Utf8JsonWriter writer, ProjectType value, JsonSerializerOptions options) 
            => writer.WriteStringValue(value.ToString());
    }

    #endregion
}