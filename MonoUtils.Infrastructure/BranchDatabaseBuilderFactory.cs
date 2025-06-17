using Microsoft.Extensions.DependencyInjection;

namespace MonoUtils.Infrastructure;

public class BranchDatabaseBuilderFactory(IServiceProvider serviceProvider)
{
    public BranchDatabaseBuilder Create() => serviceProvider.GetRequiredService<BranchDatabaseBuilder>();
}