using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("TestUtils", "TestUtils.hbs", "TestUtils.cs")]
internal class TestUtilsRenderer : BaseRenderer<ContextDefinition>
{
    protected override async Task<object> CreateViewModelAsync(ITemplateEngine engine, ContextDefinition data, CancellationToken cancellationToken)
    {
        var viewModel = new UnitTestViewModel(data);
        foreach (var method in data.RepositoryMethods)
        {
            var model = data.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
            var methodViewModel = new UnitTestMethodViewModel
            {
                IsList = method.IsList,
                MethodName = method.MethodName,
                ReturnType = method.ReturnType,
                SprocName = method.DatabaseName,
                DatabaseType = method.DatabaseType,
                Parameters = method.Parameters,
                Properties = model?.Properties ?? new(),
                SprocParameters = GetSprocParameters(method),
            };

            // todo: add support for ReturnValue parameter when NonQuery && MethodName.Contains("Insert")

            var resourceFileName = GetResourceFileName(methodViewModel, data);
            var code = await engine.ProcessAsync(resourceFileName, methodViewModel, cancellationToken);

            viewModel.Methods.Add(code);
        }

        return viewModel;
    }

    #region Private Methods

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

    private static string GetResourceFileName(UnitTestMethodViewModel method, ContextDefinition data)
    {
        if (method.IsList)
            return "UnitTestMethodQueryMany.hbs";

        return data.DTOModels.Exists(m => m.ClassName == method.ReturnType)
            ? "UnitTestMethodQuerySingle.hbs" 
            : "UnitTestMethodNonQuery.hbs"; // example: int or bool or etc...
    }

    #endregion
}