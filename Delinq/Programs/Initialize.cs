using Delinq.Parsers;
using MediatR;

namespace Delinq.Programs;

public sealed class Initialize
{
    public class Request : IRequest<string>
    {
        public string DbmlFilePath { get; set; }
        public string SettingsFilePath { get; set; }
        public bool ForceOverwrite { get; set; }

        public string DesignerFilePath => DbmlFilePath.Replace(".dbml", ".designer.cs");
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
            using (var reader = File.OpenText(request.DesignerFilePath))
            {
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    var parser = _parsers.FirstOrDefault(p => p.CanParse(line));
                    parser?.Parse(definition, reader);
                }
            }

            // serialize the definition to a json file
            var filePath = CalculateFilePath(request, definition);

            await _serializer.SerializeAsync(filePath, definition, cancellationToken);

            return filePath;
        }

        private static void ValidateRequest(Request request)
        {
            if (!File.Exists(request.DbmlFilePath))
                throw new FileNotFoundException($"File not found: {request.DbmlFilePath}");

            if (!File.Exists(request.DesignerFilePath))
                throw new FileNotFoundException($"File not found: {request.DesignerFilePath}");

            // if user specifies settings file path, make sure we don't overwrite it unless they force it
            if (string.IsNullOrEmpty(request.SettingsFilePath))
                return;

            if (File.Exists(request.SettingsFilePath) && !request.ForceOverwrite)
                throw new InvalidOperationException($"File already exists: {request.SettingsFilePath} (use -f to overwrite)");
        }

        private static string CalculateFilePath(Request request, ContextDefinition definition)
        {
            if (!string.IsNullOrEmpty(request.SettingsFilePath))
                return request.SettingsFilePath;

            // otherwise, build it from the definition
            var directory = Path.GetDirectoryName(request.DbmlFilePath);
            if (directory is null)
                throw new InvalidOperationException($"Unable to determine directory for: {request.DbmlFilePath}");

            var fileName = $"{definition.ContextName}.metadata.settings";
            return Path.Combine(directory, fileName);
        }
    }
}