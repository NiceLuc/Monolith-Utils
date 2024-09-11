using Delinq.CodeGeneration.Engine;

namespace Delinq.CodeGeneration.Renderers;

public interface IRenderer<in T>
{
    /// <summary>
    /// The key that identifies the renderer. This key is used to retrieve the renderer from the <see cref="IRendererProvider{T}"/>.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Provides a formatted string of the file name (with extension). Example: "File_{0}.txt"
    /// </summary>
    string? FileNameFormat { get; }

    /// <summary>
    /// Processes the template with the given data and returns the result as a string.
    /// </summary>
    /// <param name="engine">The service which does the template processing.</param>
    /// <param name="data">The model object used to render the template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A string which contains all replacements from the template using the data object.</returns>
    Task<string> RenderAsync(ITemplateEngine engine, T data, CancellationToken cancellationToken);
}