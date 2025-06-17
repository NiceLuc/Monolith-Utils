using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using Moq;
using SharedKernel;

namespace MonoUtils.Infrastructure.Tests;

[TestClass]
public class BranchDatabaseBuilderTests
{
    private const string BUILD_NAME = "test build";
    private const string SOLUTION_PATH = @"c:\dummy\solution_path.sln";
    private const string PROJECT_PATH = @"c:\dummy\project_path.csproj";
    private const string WIX_PROJECT_PATH = @"c:\dummy\wix_project_path.wixproj";

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);
    private Mock<ILoggerFactory> _loggerFactory;
    private Mock<IFileStorage> _fileStorage;
    private UniqueNameResolver _resolver;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _loggerFactory = _mockRepository.Create<ILoggerFactory>();
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(false);

        _resolver = new UniqueNameResolver();
    }

    /*
    #region AddError Tests
    [TestMethod]
    [DataRow(RecordType.Solution, "solution", ErrorSeverity.Info)]
    [DataRow(RecordType.Solution, "solution", ErrorSeverity.Warning)]
    [DataRow(RecordType.Solution, "solution", ErrorSeverity.Critical)]
    [DataRow(RecordType.Project, "project", ErrorSeverity.Info)]
    [DataRow(RecordType.Project, "project", ErrorSeverity.Warning)]
    [DataRow(RecordType.Project, "project", ErrorSeverity.Critical)]
    [DataRow(RecordType.WixProject, "wix", ErrorSeverity.Info)]
    [DataRow(RecordType.WixProject, "wix", ErrorSeverity.Warning)]
    [DataRow(RecordType.WixProject, "wix", ErrorSeverity.Critical)]
    public void AddError_HappyPath(RecordType type, string name, ErrorSeverity severity)
    {
        // assemble
        var builder = CreateBuilder();
        var error = new ErrorRecord(type, name, "test", severity);
        builder.AddError(error);

        // act
        var db = builder.CreateDatabase();
        var result = db.Errors[0];

        // assert
        Assert.AreEqual(result.Message, "test");
    }
    #endregion
    */

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetOrAddSolution_CreatesNewRecord(bool expectedExists)
    {
        // assemble
        const string expectedName = "solution_path";
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(expectedExists);
        var builder = CreateBuilder();

        // act
        var solution = builder.GetOrAddSolution(SOLUTION_PATH);

        // assert
        Assert.AreEqual(false, solution.IsRequired);
        Assert.AreEqual(expectedName, solution.Name);
        Assert.AreEqual(expectedExists, solution.DoesExist);
        Assert.AreEqual(SOLUTION_PATH, solution.Path);
    }

    [TestMethod]
    public void GetOrAddSolution_CachesResult()
    {
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        var builder = CreateBuilder();

        // act
        var a = builder.GetOrAddSolution(SOLUTION_PATH);
        var b = builder.GetOrAddSolution(SOLUTION_PATH);

        // assert
        Assert.IsNotNull(a);
        Assert.AreSame(a, b);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetOrAddProject_CreatesNewRecord(bool expectedExists)
    {
        // assemble
        const string expectedName = "project_path";
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(expectedExists);
        var builder = CreateBuilder();

        // act
        var project = builder.GetOrAddProject(PROJECT_PATH);

        // assert
        Assert.AreEqual(false, project.IsRequired);
        Assert.AreEqual(expectedExists, project.DoesExist);
        Assert.AreEqual(expectedName, project.Name);
        Assert.AreEqual(PROJECT_PATH, project.Path);
    }

    [TestMethod]
    public void GetOrAddProject_CachesResult()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var builder = CreateBuilder();

        // act
        var a = builder.GetOrAddProject(PROJECT_PATH);
        var b = builder.GetOrAddProject(PROJECT_PATH);

        // assert
        Assert.IsNotNull(a);
        Assert.AreSame(a, b);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetOrAddWixProject_CreatesNewRecord(bool expectedExists)
    {
        // assemble
        const string expectedName = "wix_project_path";
        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(expectedExists);
        var builder = CreateBuilder();

        // act
        var project = builder.GetOrAddWixProject(WIX_PROJECT_PATH);

        // assert
        Assert.AreEqual(false, project.IsRequired);
        Assert.AreEqual(expectedExists, project.DoesExist);
        Assert.AreEqual(expectedName, project.Name);
        Assert.AreEqual(WIX_PROJECT_PATH, project.Path);
    }

    [TestMethod]
    public void GetOrAddWixProject_CachesResult()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(false);
        var builder = CreateBuilder();

        // act
        var a = builder.GetOrAddWixProject(WIX_PROJECT_PATH);
        var b = builder.GetOrAddWixProject(WIX_PROJECT_PATH);

        // assert
        Assert.IsNotNull(a);
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void CreateDatabase_SetsRequiredSolutions()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        var builder = CreateBuilder();
        var solution = builder.GetOrAddSolution(SOLUTION_PATH);
        solution.Builds.Add(BUILD_NAME);

        // act
        var db = builder.CreateDatabase();
        var result = db.Solutions[0];

        // assert
        Assert.IsTrue(result.IsRequired);
    }

    [TestMethod]
    public void CreateDatabase_SetsRequiredProjects()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(true);
        var builder = CreateBuilder();
        var solution = builder.GetOrAddSolution(SOLUTION_PATH);
        solution.Builds.Add(BUILD_NAME);

        // assign the project to the solution
        var project = builder.GetOrAddProject(PROJECT_PATH);
        solution.Projects.Add(new SolutionProjectReference(project.Name, ProjectType.Unknown));

        // act
        var db = builder.CreateDatabase();
        var result = db.Projects[0];

        // assert
        Assert.IsTrue(result.IsRequired);
    }

    [TestMethod]
    public void CreateDatabase_SetsRequiredWixProjects()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(true);
        var builder = CreateBuilder();
        var solution = builder.GetOrAddSolution(SOLUTION_PATH);
        solution.Builds.Add(BUILD_NAME);

        // assign the wix project to the solution
        var wixProject = builder.GetOrAddWixProject(WIX_PROJECT_PATH);
        solution.WixProjects.Add(wixProject.Name);

        // act
        var db = builder.CreateDatabase();
        var result = db.WixProjects[0];

        // assert
        Assert.IsTrue(result.IsRequired);
    }


    private BranchDatabaseBuilder CreateBuilder()
    {
        var solutionProvider = new RecordProvider<SolutionRecord>(_resolver, _fileStorage.Object);
        var projectProvider = new RecordProvider<ProjectRecord>(_resolver, _fileStorage.Object);
        var wixProjectProvider = new RecordProvider<WixProjectRecord>(_resolver, _fileStorage.Object);
        return new BranchDatabaseBuilder(solutionProvider, projectProvider, wixProjectProvider);
    }
}