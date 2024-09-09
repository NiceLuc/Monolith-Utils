namespace GVLinQOptimizer.Renderers;

public interface ITemplateEngine
{
    Task<string> ProcessAsync(string resourceFileName, object data, CancellationToken cancellationToken);
}