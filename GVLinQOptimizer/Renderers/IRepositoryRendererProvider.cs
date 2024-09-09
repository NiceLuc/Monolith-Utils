namespace GVLinQOptimizer.Renderers;

public interface IRepositoryRendererProvider
{
    IRenderer<ContextDefinition> GetIRepositorySettingsRendererAsync();
    IRenderer<ContextDefinition> GetIRepositoryRendererAsync();
    IRenderer<ContextDefinition> GetRepositorySettingsRendererAsync();
    IRenderer<ContextDefinition> GetRepositoryRendererAsync();
    IRenderer<ContextDefinition> GetDataContextRendererAsync();
}