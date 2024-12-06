using MonoUtils.Domain;
using Mustache;

namespace MonoUtils.Infrastructure;

public class HandlebarsTemplateEngine(FormatCompiler compiler) : ITemplateEngine
{
    public string ProcessTemplate(string template, object data)
    {
        var generator = compiler.Compile(template);
        return generator.Render(data);
    }
}