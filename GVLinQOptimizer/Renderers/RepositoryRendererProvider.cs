namespace GVLinQOptimizer.Renderers;

internal class RepositoryRendererProvider : BaseRendererProvider, IRepositoryRendererProvider
{
    public RepositoryRendererProvider(IEnumerable<IRenderer<ContextDefinition>> renders) 
        : base(renders) { }

    public IRenderer<ContextDefinition> GetIRepositorySettingsRendererAsync() => GetRenderer("IRepositorySettings.hbs");
    public IRenderer<ContextDefinition> GetIRepositoryRendererAsync() => GetRenderer("IRepository.hbs");
    public IRenderer<ContextDefinition> GetRepositorySettingsRendererAsync() => GetRenderer("RepositorySettings.hbs");
    public IRenderer<ContextDefinition> GetRepositoryRendererAsync() => GetRenderer("Repository.hbs");
    public IRenderer<ContextDefinition> GetDataContextRendererAsync() => GetRenderer("DataContext.hbs");
}