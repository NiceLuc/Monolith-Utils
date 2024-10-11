namespace Delinq;

public interface IContextDefinitionSerializer<T>
{
    Task SerializeAsync(string filePath, T definition, CancellationToken cancellationToken);
    Task<T> DeserializeAsync(string filePath, CancellationToken cancellationToken);
}