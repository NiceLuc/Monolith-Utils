using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using Moq;
using SharedKernel;

namespace MonoUtils.UseCases.Tests;

public static class TestUtils
{
    public static BranchDatabaseBuilder CreateBuilder(Mock<IFileStorage> fileStorage)
    {
        var resolver = new UniqueNameResolver();
        var solutionProvider = new RecordProvider<SolutionRecord>(resolver, fileStorage.Object);
        var projectProvider = new RecordProvider<ProjectRecord>(resolver, fileStorage.Object);
        var wixProjectProvider = new RecordProvider<WixProjectRecord>(resolver, fileStorage.Object);
        return new BranchDatabaseBuilder(solutionProvider, projectProvider, wixProjectProvider);
    }
}