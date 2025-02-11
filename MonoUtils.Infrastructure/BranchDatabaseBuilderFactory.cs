using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using SharedKernel;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilderFactory(IServiceProvider serviceProvider)
{
    public BranchDatabaseBuilder Create(BuildDefinition[] builds)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        var resolver = serviceProvider.GetRequiredService<UniqueNameResolver>();
        return new BranchDatabaseBuilder(loggerFactory, fileStorage, resolver, builds);
    }
}