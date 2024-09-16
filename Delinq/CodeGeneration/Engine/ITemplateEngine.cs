namespace Delinq.CodeGeneration.Engine;

public interface ITemplateEngine
{
    string ProcessTemplate(string template, object data);
}