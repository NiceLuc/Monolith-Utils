using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("UnitTests", "UnitTests.hbs", "{0}RepositoryTests.cs")]
internal class UnitTestsRenderer : BaseRenderer<ContextDefinition>
{
    protected override async Task<object> CreateViewModelAsync(ITemplateEngine engine, ContextDefinition data, CancellationToken cancellationToken)
    {
        var viewModel = new UnitTestViewModel(data);
        foreach (var method in data.RepositoryMethods)
        {
            // find the model that matches the method's return type
            var model = data.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
            var methodViewModel = CreateMethodViewModel(method, model);

            var resourceFileName = GetResourceFileName(methodViewModel, data);
            var code = await engine.ProcessAsync(resourceFileName, methodViewModel, cancellationToken);

            viewModel.Methods.Add(code);
        }

        return viewModel;
    }

    #region Private Methods

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