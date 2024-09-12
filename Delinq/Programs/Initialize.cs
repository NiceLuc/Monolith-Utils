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

    public class Handler(
        IEnumerable<IParser<ContextDefinition>> parsers,
        IContextDefinitionSerializer serializer)
        : IRequestHandler<Request, string>
    {
        private static string[] TokensForReturnValueParameter = {"Add", "Create", "Insert"};

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var definition = new ContextDefinition();

            // parse the designer file
            using (var reader = File.OpenText(request.DesignerFilePath))
            {
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    var parser = parsers.FirstOrDefault(p => p.CanParse(line));
                    parser?.Parse(definition, reader);
                }
            }

            // loop through all methods on the definition and determine if they require return value parameters
            foreach (var method in definition.RepositoryMethods) 
                method.HasReturnParameter = DoesMethodRequireReturnParameter(method);

            // serialize the definition to a json file
            var filePath = CalculateFilePath(request, definition);

            await serializer.SerializeAsync(filePath, definition, cancellationToken);

            return filePath;
        }

        #region Private Methods

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

        private static bool DoesMethodRequireReturnParameter(MethodDefinition method)
        {
            if (method.DatabaseType != "NonQuery") 
                return false;

            if (!TokensForReturnValueParameter.Any(token => method.MethodName.Contains(token))) 
                return false;

            // if the method name contains one of the tokens, we need to check if there are out parameters
            return method.Parameters.All(p => p.ParameterDirection != "Output");

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

        #endregion
    }
}