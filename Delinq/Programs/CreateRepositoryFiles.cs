using Delinq.CodeGeneration;
using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;
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
                FilterMethodsAndDTOModels(definition, request.MethodName);

            await ProcessTemplate("IRepositorySettings.hbs", "I{0}RepositorySettings.cs");
            await ProcessTemplate("IRepository.hbs", "I{0}Repository.cs");
            await ProcessTemplate("RepositorySettings.hbs", "{0}RepositorySettings.cs");
            await ProcessTemplate("DTOModels.hbs", "{0}DataModels.cs");

            var viewModel = await CreateViewModelAsync(definition, cancellationToken);
            await ProcessTemplate("Repository.hbs", "{0}Repository.cs", viewModel);
            await ProcessTemplate("DataContext.hbs", "{0}DataContext.cs");

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

        private async Task<object> CreateViewModelAsync(ContextDefinition data, CancellationToken cancellationToken)
        {
            var viewModel = new RepositoryViewModel(data);
            foreach (var method in data.RepositoryMethods)
            {
                var model = data.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
                var methodViewModel = CreateMethodViewModel(method, model);

                var resourceFileName = GetResourceFileName(methodViewModel, data);
                var code = await templateEngine.ProcessAsync(resourceFileName, methodViewModel, cancellationToken);

                viewModel.Methods.Add(code);
            }

            return viewModel;
        }

        private static RepositoryMethodViewModel CreateMethodViewModel(MethodDefinition method, DTOClassDefinition? model)
        {
            return new RepositoryMethodViewModel
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

        private static List<RepositoryParameterViewModel> GetSprocParameters(MethodDefinition method)
        {
            return method.Parameters.Select(parameter => new RepositoryParameterViewModel
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

        private static RepositoryParameterViewModel CreateReturnParameter(MethodDefinition method)
        {
            return new RepositoryParameterViewModel
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

        private static string GetResourceFileName(RepositoryMethodViewModel method, ContextDefinition data)
        {
            if (method.IsList)
                return "RepositoryMethodQueryMany.hbs";

            return data.DTOModels.Exists(m => m.ClassName == method.ReturnType)
                ? "RepositoryMethodQuerySingle.hbs" 
                : "RepositoryMethodNonQuery.hbs"; // example: int or bool or etc...
        }

        #endregion
    }
}