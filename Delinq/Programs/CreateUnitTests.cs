using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;
using MediatR;

namespace Delinq.Programs;

public sealed class CreateUnitTests
{
    public class Request : IRequest<string>
    {
        public string SettingsFilePath { get; set; }
        public string OutputDirectory { get; set; }
        public string MethodName { get; init; }
    }

    public class Handler(
        IContextDefinitionSerializer definitionSerializer,
        ITemplateProvider templateProvider,
        ITemplateEngine templateEngine,
        IFileStorage fileStorage)
        : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(request.OutputDirectory))
                Directory.CreateDirectory(request.OutputDirectory);

            var definition = await definitionSerializer.DeserializeAsync(request.SettingsFilePath, cancellationToken);
            if (!string.IsNullOrEmpty(request.MethodName))
                FilterMethods(definition, request.MethodName);

            var viewModel = await CreateViewModelAsync(definition, cancellationToken);
            await ProcessTemplate("UnitTests.hbs", "{0}RepositoryTests.cs", viewModel);
            await ProcessTemplate("TestUtils.hbs", "TestUtils.cs");

            return request.OutputDirectory;

            async Task ProcessTemplate(string resourceName, string fileNameFormat, object? data = null)
            {
                var template = await templateProvider.GetTemplateAsync(resourceName, cancellationToken);
                var generatedCode = templateEngine.ProcessTemplate(template, data ?? definition);
                var fileName = string.Format(fileNameFormat, definition.ContextName);
                var filePath = Path.Combine(request.OutputDirectory, fileName);
                await fileStorage.WriteAllTextAsync(filePath, generatedCode, cancellationToken);
            }
        }

        #region Private Methods

        private static void FilterMethods(ContextDefinition definition, string methodName)
        {
            var method = definition.RepositoryMethods.FirstOrDefault(m =>
                m.MethodName.Equals(methodName, StringComparison.InvariantCultureIgnoreCase));

            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found.");

            definition.RepositoryMethods = [method];
        }

        private async Task<object> CreateViewModelAsync(ContextDefinition data, CancellationToken cancellationToken)
        {
            var viewModel = new UnitTestViewModel(data);
            foreach (var method in data.RepositoryMethods)
            {
                // find the model that matches the method's return type
                var model = data.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
                var methodViewModel = CreateMethodViewModel(method, model);

                var resourceFileName = GetResourceFileName(methodViewModel, data);
                var code = await templateEngine.ProcessAsync(resourceFileName, methodViewModel, cancellationToken);

                viewModel.Methods.Add(code);
            }

            return viewModel;
        }

        private static UnitTestMethodViewModel CreateMethodViewModel(MethodDefinition method, DTOClassDefinition? model)
        {
            return new UnitTestMethodViewModel
            {
                IsList = method.IsList,
                MethodName = method.MethodName,
                ReturnType = method.ReturnType,
                SprocName = method.DatabaseName,
                DatabaseType = method.DatabaseType,
                Parameters = method.Parameters,
                Properties = model?.Properties ?? new(),
                SprocParameters = GetSprocParameters(method),
                ReturnValueParameter = method.HasReturnParameter 
                    ? CreateReturnParameter(method) : null
            };
        }

        private static List<UnitTestParameterViewModel> GetSprocParameters(MethodDefinition method)
        {
            return method.Parameters.Select(parameter => new UnitTestParameterViewModel
            {
                // method parameter details
                MethodParameterName = parameter.ParameterName,
                MethodParameterType = parameter.ParameterType,

                // sproc parameter details
                SprocParameterName = parameter.SprocParameterName,
                SprocParameterType = parameter.SqlDbType,
                SprocParameterDirection = parameter.ParameterDirection,
                SprocParameterLength = parameter.DatabaseLength,
                HasStringLength = HasStringLength(parameter),
                ShouldCaptureResult = parameter.IsRef,
                IsInputParameter = IsInputParameter(parameter)

            }).ToList();

            bool IsInputParameter(ParameterDefinition parameter) =>
                !parameter.IsRef && parameter.ParameterDirection.Equals("Input",
                    StringComparison.InvariantCultureIgnoreCase);

            bool HasStringLength(ParameterDefinition parameter) =>
                !string.IsNullOrEmpty(parameter.DatabaseLength) &&
                !parameter.DatabaseLength.Equals("max",
                    StringComparison.InvariantCultureIgnoreCase);
        }

        private static UnitTestParameterViewModel CreateReturnParameter(MethodDefinition method)
        {
            return new UnitTestParameterViewModel
            {
                MethodParameterName = "returnValue",
                MethodParameterType = "int",
                SprocParameterName = "ReturnValue",
                SprocParameterType = "Int",
                SprocParameterDirection = "ReturnValue",
                ShouldCaptureResult = true,
                IsInputParameter = false
            };
        }

        private static string GetResourceFileName(UnitTestMethodViewModel method, ContextDefinition data)
        {
            // example: int or bool or etc...
            if (method.DatabaseType == "NonQuery") 
                return "UnitTestMethodNonQuery.hbs";

            // returns a model type
            if (!data.DTOModels.Exists(m => m.ClassName == method.ReturnType))
                throw new InvalidOperationException($"{data.ContextName}.{method.MethodName} requires {method.ReturnType} model.");

            return method.IsList 
                ? "UnitTestMethodQueryMany.hbs" 
                : "UnitTestMethodQuerySingle.hbs";
        }

        #endregion
    }
}
