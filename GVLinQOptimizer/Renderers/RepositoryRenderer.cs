using GVLinQOptimizer.Renderers.ViewModels;

namespace GVLinQOptimizer.Renderers;

[HandlebarsTemplateModel("Repository.hbs", "{0}Repository.cs")]
internal class RepositoryRenderer : BaseRenderer<ContextDefinition>
{
    private readonly IRenderer<MethodDefinition> _methodRenderer;

    public RepositoryRenderer(IRenderer<MethodDefinition> methodRenderer)
    {
        _methodRenderer = methodRenderer;
    }

    public override async Task<object> ToViewModelAsync(ITemplateEngine engine, ContextDefinition data, CancellationToken cancellationToken)
    {
        var viewModel = new RepositoryViewModel(data);
        foreach (var method in data.RepositoryMethods)
        {
            var code = await _methodRenderer.RenderAsync(engine, method, cancellationToken);
            viewModel.Methods.Add(code);
        }

        return viewModel;
    }

    public override async Task<string> RenderAsync(ITemplateEngine engine, object data, CancellationToken cancellationToken)
    {

        return await engine.ProcessAsync(ResourceFileName, data, cancellationToken);
    }
}