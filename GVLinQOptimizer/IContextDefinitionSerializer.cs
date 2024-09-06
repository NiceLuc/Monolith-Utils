namespace GVLinQOptimizer;

public interface IContextDefinitionSerializer
{
    Task SerializeAsync(string filePath, ContextDefinition definition, CancellationToken cancellationToken);
    Task<ContextDefinition> DeserializeAsync(string filePath, CancellationToken cancellationToken);
}