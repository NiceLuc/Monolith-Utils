using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;
using MediatR;

namespace Delinq.Programs;

public sealed class CreateUnitTests
{
    public class Request : IRequest<string>
    {
        public string ContextName { get; init; }
        public string SettingsFilePath { get; set; }
        public string OutputDirectory { get; set; }
        public string MethodName { get; init; }
    }

    public class Handler(
        IConfigSettingsBuilder settingsBuilder,
        IDefinitionSerializer<ContextDefinition> definitionSerializer,
        ITemplateProvider templateProvider,
        ITemplateEngine templateEngine,
        IFileStorage fileStorage)
        : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            await ConfigureRequestAsync(request, cancellationToken);
            ValidateRequest(request);

            var definition = await definitionSerializer.DeserializeAsync(request.SettingsFilePath, cancellationToken);
            if (!string.IsNullOrEmpty(request.MethodName))
                FilterMethods(definition, request.MethodName);

            await ProcessTemplate("TestUtils.hbs", "TestUtils.cs");

            var viewModel = await CreateViewModelAsync(definition, cancellationToken);
            await ProcessTemplate("UnitTests.hbs", "{0}RepositoryTests.cs", viewModel);

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

        private async Task ConfigureRequestAsync(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(request.ContextName, string.Empty, cancellationToken);

            if (string.IsNullOrEmpty(request.SettingsFilePath))
                request.SettingsFilePath = settings.TempMetaDataFilePath;

            if (string.IsNullOrEmpty(request.OutputDirectory))
                request.OutputDirectory = settings.TempTestDirectoryPath;
        }

        private static void ValidateRequest(Request request)
        {
            if (!File.Exists(request.SettingsFilePath))
                throw new FileNotFoundException("File does not exist: " + request.SettingsFilePath);

            if (!Directory.Exists(request.OutputDirectory))
                Directory.CreateDirectory(request.OutputDirectory);
        }


        private static void FilterMethods(ContextDefinition definition, string methodName)
        {
            var method = definition.RepositoryMethods.FirstOrDefault(m =>
                m.MethodName.Equals(methodName, StringComparison.InvariantCultureIgnoreCase));

            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found.");

            definition.RepositoryMethods = [method];
        }

        private async Task<object> CreateViewModelAsync(ContextDefinition definition, CancellationToken cancellationToken)
        {
            var viewModel = new UnitTestViewModel(definition);
            foreach (var method in definition.RepositoryMethods)
            {
                // find the model that matches the method's return type
                var methodViewModel = CreateMethodViewModel(method, definition);

                var resourceFileName = GetResourceFileName(methodViewModel, definition);
                var template = await templateProvider.GetTemplateAsync(resourceFileName, cancellationToken);
                var code = templateEngine.ProcessTemplate(template, methodViewModel);

                viewModel.Methods.Add(code);
            }

            return viewModel;
        }

        private static UnitTestMethodViewModel CreateMethodViewModel(MethodDefinition method, ContextDefinition definition)
        {
            var model = definition.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
            var isList = method is {DatabaseType: "Query", IsList: true};

            return new UnitTestMethodViewModel
            {
                IsList = method.IsList,
                MethodName = method.MethodName,
                ReturnType = method.ReturnType,
                SprocName = method.DatabaseName,
                DatabaseType = method.DatabaseType,
                Parameters = CreateParameters(method),
                Properties = isList
                    ? CreateEnumerableProperties(method, model)
                    : CreateProperties(method, model),
                ReturnValueParameter = method.HasReturnParameter
                    ? CreateReturnParameter(method)
                    : null
            };
        }

        private static List<UnitTestParameterViewModel> CreateParameters(MethodDefinition method)
        {
            return (from p in method.Parameters
                let fakeValue = GetFakeValue(p.ParameterType, p.ParameterName)
                select new UnitTestParameterViewModel
                {
                    ParameterName = p.ParameterName,
                    ParameterType = p.ParameterType,
                    IsNullable = p.IsNullable,
                    IsRef = p.IsRef,
                    IsInputParameter = IsInputParameter(p),
                    InitialValue = GetFakeValue(p.ParameterType, p.ParameterName),
                    FakeValue = p.IsRef ? p.ParameterName : GetFakeValue(p.ParameterType, p.ParameterName),
                }).ToList();
        }

        private static List<UnitTestPropertyViewModel> CreateEnumerableProperties(MethodDefinition method, DTOClassDefinition? model)
        {
            if (model == null)
                return new();

            return model.Properties.Select(p => new UnitTestPropertyViewModel
            {
                PropertyName = p.PropertyName,
                PropertyType = p.PropertyType,
                FakeValue = p.PropertyType.Replace("?", "") switch
                {
                    "string" => $"\"{p.PropertyName} {{x}}",
                    "char" => "'x'",
                    "bool" => "x % 2 == 0",
                    "int" => "x",
                    "short" => "x",
                    "double" => "x",
                    "long" => "x",
                    "decimal" => "x",
                    "DateTime" => "DateTime.Now.AddDays(x)",
                    "Guid" => "Guid.NewGuid()",
                    "byte" => "0b1",
                    "byte[]" => "Enumerable.Empty<byte>().ToArray()",
                    _ => throw new ArgumentOutOfRangeException(p.PropertyType)
                }
            }).ToList();
        }

        private static List<UnitTestPropertyViewModel> CreateProperties(MethodDefinition method, DTOClassDefinition? model)
        {
            if (model == null)
                return new();

            return model.Properties.Select(p => new UnitTestPropertyViewModel
            {
                PropertyName = p.PropertyName,
                PropertyType = p.PropertyType,
                FakeValue = p.PropertyType.Replace("?", "") switch
                {
                    "string" => $"\"{p.PropertyName}\"",
                    "char" => "'x'",
                    "bool" => "false",
                    "int" => "1",
                    "short" => "1",
                    "double" => "1",
                    "long" => "1L",
                    "decimal" => "1d",
                    "DateTime" => "DateTime.Now",
                    "Guid" => "Guid.NewGuid()",
                    "byte[]" => "Enumerable.Empty<byte>().ToArray()",
                    "byte" => "0b1",
                    _ => throw new ArgumentOutOfRangeException(p.PropertyType)
                }
            }).ToList();
        }

        private static string GetFakeValue(string parameterType, string parameterName)
        {
            return parameterType.Replace("?", "") switch
            {
                "string" => $"\"{parameterName}\"",
                "char" => "'x'",
                "bool" => "false",
                "int" => "1",
                "short" => "1",
                "double" => "1",
                "long" => "1L",
                "decimal" => "1d",
                "DateTime" => "DateTime.Now",
                "Guid" => "Guid.NewGuid()",
                "byte[]" => "Enumerable.Empty<byte>().ToArray()",
                "byte" => "0b1",
                _ => throw new ArgumentOutOfRangeException(parameterType)
            };
        }

        private static bool IsInputParameter(ParameterDefinition parameter) =>
            !parameter.IsRef && parameter.ParameterDirection.Equals("Input",
                StringComparison.InvariantCultureIgnoreCase);

        private static UnitTestParameterViewModel CreateReturnParameter(MethodDefinition method)
        {
            return new UnitTestParameterViewModel
            {
                ParameterName = "returnValue",
                ParameterType = "int",
                IsRef = true,
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
