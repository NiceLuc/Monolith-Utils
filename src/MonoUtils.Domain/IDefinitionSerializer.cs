namespace MonoUtils.Domain;

public interface IDefinitionSerializer<T>
{
    Task SerializeAsync(string filePath, T definition, CancellationToken cancellationToken);
    Task<T> DeserializeAsync(string filePath, CancellationToken cancellationToken);
}