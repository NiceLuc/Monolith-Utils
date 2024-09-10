namespace GVLinQOptimizer.CodeGeneration.Engine;

public interface ITemplateProvider
{
    Task<string> GetTemplateAsync(string resourceFileName, CancellationToken cancellationToken);
}