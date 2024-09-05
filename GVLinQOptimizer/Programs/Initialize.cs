using System.Text.Json;
using GVLinQOptimizer.Parsers;
using MediatR;

namespace GVLinQOptimizer.Programs;

public sealed class Initialize
{
    public class Request : IRequest<string>
    {
        public string DesignerFilePath { get; set; }
        public string SettingsFilePath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler : IRequestHandler<Request, string>
    {
        private readonly IEnumerable<IParser<ContextDefinition>> _parsers;

        public Handler()
        {
            var parsers = new List<IParser<ContextDefinition>>
            {
                new NamespaceParser(),
                new ContextClassParser(),
                new MethodParser(),
                new DTOClassParser(),
            };

            _parsers = parsers.AsReadOnly();
        }

        public Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var definition = new ContextDefinition();

            // parse the designer file
            using (var sr = File.OpenText(request.DesignerFilePath))
            {
                while (sr.ReadLine() is { } line)
                {
                    var parser = _parsers.FirstOrDefault(p => p.CanParse(line));
                    parser?.Parse(definition, sr);
                }
            }

            // serialize the definition to a json file
            var filePath = CalculateFilePath(request, definition);
            var prettified = new JsonSerializerOptions {WriteIndented = true};
            var serialized = JsonSerializer.Serialize(definition, prettified);
            File.WriteAllText(filePath, serialized);

            return Task.FromResult(filePath);
        }

        private static void ValidateRequest(Request request)
        {
            if (!File.Exists(request.DesignerFilePath))
                throw new FileNotFoundException($"File not found: {request.DesignerFilePath}");

            if (File.Exists(request.SettingsFilePath) && !request.ForceOverwrite)
                throw new InvalidOperationException(
                    $"File already exists: {request.SettingsFilePath} (use -f to overwrite)");
        }

        private static string CalculateFilePath(Request request, ContextDefinition definition)
        {
            if (!string.IsNullOrEmpty(request.SettingsFilePath))
                return request.SettingsFilePath;

            // otherwise, build it from the definition
            var directory = Path.GetDirectoryName(request.DesignerFilePath);
            if (directory is null)
                throw new InvalidOperationException($"Unable to determine directory for: {request.DesignerFilePath}");

            var fileName = $"{definition.ContextName}.metadata.settings";
            return Path.Combine(directory, fileName);
        }
    }
}