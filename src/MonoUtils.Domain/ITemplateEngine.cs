namespace MonoUtils.Domain;

public interface ITemplateEngine
{
    string ProcessTemplate(string template, object data);
}