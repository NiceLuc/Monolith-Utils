using Delinq.CodeGeneration.Engine;
using Delinq.CodeGeneration.ViewModels;

namespace Delinq.CodeGeneration.Renderers;

[HandlebarsTemplateModel("Repository", "Repository.hbs", "{0}Repository.cs")]
internal class RepositoryRenderer : BaseRenderer<ContextDefinition>
{
    protected override async Task<object> CreateViewModelAsync(ITemplateEngine engine, ContextDefinition data, CancellationToken cancellationToken)
    {
        var viewModel = new RepositoryViewModel(data);
        foreach (var method in data.RepositoryMethods)
        {
            var model = data.DTOModels.FirstOrDefault(m => m.ClassName == method.ReturnType);
            var methodViewModel = CreateMethodViewModel(method, model);

            var resourceFileName = GetResourceFileName(methodViewModel, data);
            var code = await engine.ProcessAsync(resourceFileName, methodViewModel, cancellationToken);

            viewModel.Methods.Add(code);
        }

        return viewModel;
    }

    #region Private Methods

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