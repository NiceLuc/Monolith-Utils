using Delinq.Parsers;
using MediatR;
using SharedKernel;

namespace Delinq.Programs;

public sealed class Initialize
{
    public class Request : IRequest<string>
    {
        public string ContextName { get; set; }
        public string BranchName { get; set; }

        public string DbmlFilePath { get; set; }
        public string DesignerFilePath { get; set; }
        public string SettingsFilePath { get; set; }
        public bool ForceOverwrite { get; set; }
    }

    public class Handler(
        IConfigSettingsBuilder settingsBuilder,
        IEnumerable<IParser<ContextDefinition>> parsers,
        IDefinitionSerializer<ContextDefinition> serializer)
        : IRequestHandler<Request, string>
    {
        private static readonly string[] TokensForReturnValueParameter = ["Add", "Create", "Insert"];

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            await ConfigureRequestAsync(request, cancellationToken);
            ValidateRequest(request);

            var definition = new ContextDefinition();

            // parse the designer file using the parser implementations created for ContextDefinition
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
            await serializer.SerializeAsync(request.SettingsFilePath, definition, cancellationToken);
            return request.SettingsFilePath;
        }

        #region Private Methods

        private async Task ConfigureRequestAsync(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(request.ContextName, request.BranchName, cancellationToken);

            if (string.IsNullOrEmpty(request.DbmlFilePath))
                request.DbmlFilePath = settings.TfsDbmlFilePath;

            if (string.IsNullOrEmpty(request.DesignerFilePath))
                request.DesignerFilePath = settings.TfsDesignerFilePath;

            if (string.IsNullOrEmpty(request.SettingsFilePath))
                request.SettingsFilePath = settings.TempMetaDataFilePath;
        }

        private static void ValidateRequest(Request request)
        {
            if (!File.Exists(request.DbmlFilePath))
                throw new FileNotFoundException($"File not found: {request.DbmlFilePath}");

            if (!File.Exists(request.DesignerFilePath))
                throw new FileNotFoundException($"File not found: {request.DesignerFilePath}");

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
            // if there are, we don't need to add a return parameter, as it's handled by a 'ref' parameter
            return method.Parameters.All(p => p.ParameterDirection != "Output");

        }

        #endregion
    }
}