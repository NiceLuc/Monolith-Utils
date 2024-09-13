namespace Delinq.CodeGeneration.Engine;

public interface ITemplateEngine
{
    [Obsolete("Do not use this method. Instead favor using ProcessTemplate() method.")]
    Task<string> ProcessAsync(string resourceFileName, object data, CancellationToken cancellationToken);

    string ProcessTemplate(string templateString, object data);
}