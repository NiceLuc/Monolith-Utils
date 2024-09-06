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
        private readonly IContextDefinitionSerializer _serializer;

        public Handler(IEnumerable<IParser<ContextDefinition>> parsers, IContextDefinitionSerializer serializer)
        {
            _parsers = parsers;
            _serializer = serializer;
        }

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var definition = new ContextDefinition();

            // parse the designer file
            using (var sr = File.OpenText(request.DesignerFilePath))
            {
                while (await sr.ReadLineAsync(cancellationToken) is { } line)
                {
                    var parser = _parsers.FirstOrDefault(p => p.CanParse(line));
                    parser?.Parse(definition, sr);
                }
            }

            // serialize the definition to a json file
            var filePath = CalculateFilePath(request, definition);

            await _serializer.SerializeAsync(filePath, definition, cancellationToken);

            return filePath;
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