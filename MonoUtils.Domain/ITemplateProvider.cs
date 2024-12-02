namespace MonoUtils.Domain;

public interface ITemplateProvider
{
    Task<string> GetTemplateAsync(string resourceFileName, CancellationToken cancellationToken);
}