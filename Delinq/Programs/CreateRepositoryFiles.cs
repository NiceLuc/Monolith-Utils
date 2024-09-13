using Delinq.CodeGeneration;
using Delinq.CodeGeneration.Engine;
using MediatR;

namespace Delinq.Programs;

public sealed class CreateRepositoryFiles
{
    public class Request : IRequest<string>
    {
        public string SettingsFilePath { get; init; }
        public string OutputDirectory { get; init; }
        public string MethodName { get; init; }
    }

    public class Handler(
        IContextDefinitionSerializer definitionSerializer,
        ITemplateEngine templateEngine,
        IRendererProvider<ContextDefinition> provider) 
        : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(request.OutputDirectory))
                Directory.CreateDirectory(request.OutputDirectory);

            var definition = await definitionSerializer.DeserializeAsync(request.SettingsFilePath, cancellationToken);
            if (!string.IsNullOrEmpty(request.MethodName)) 
                FilterMethodsAndDTOModels(definition, request.MethodName);

            await ProcessTemplate("RepositorySettingsInterface");
            await ProcessTemplate("RepositoryInterface");
            await ProcessTemplate("RepositorySettings");

            await ProcessTemplate("DTOModels");
            await ProcessTemplate("Repository");
            await ProcessTemplate("DataContext");

            return request.OutputDirectory;

            async Task ProcessTemplate(string rendererKey)
            {
                var renderer = provider.GetRenderer(rendererKey);
                if (string.IsNullOrEmpty(renderer.FileNameFormat))
                    throw new InvalidOperationException($"FileNameFormat attribute required for {renderer.GetType()}");

                var generatedCode = await renderer.RenderAsync(templateEngine, definition, cancellationToken);
                var fileName = string.Format(renderer.FileNameFormat, definition.ContextName);
                var filePath = Path.Combine(request.OutputDirectory, fileName);
                await File.WriteAllTextAsync(filePath, generatedCode, cancellationToken);
            }
        }

        #region Private Methods

        private static void FilterMethodsAndDTOModels(ContextDefinition definition, string methodName)
        {
            var method = definition.RepositoryMethods.FirstOrDefault(m =>
                m.MethodName.Equals(methodName, StringComparison.InvariantCultureIgnoreCase));

            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found.");

            var model = definition.DTOModels.FirstOrDefault(m =>
                m.ClassName.Equals(method.ReturnType, StringComparison.InvariantCultureIgnoreCase));

            definition.RepositoryMethods = [method];
            definition.DTOModels = model == null ? [] : [model];
        }

        #endregion
    }
}