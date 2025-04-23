using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;
using Moq;
using SharedKernel;

namespace MonoUtils.UseCases.Tests;

[TestClass]
public class StandardProjectFileScannerTests
{
    private const string PROJECT_PATH = @"c:\dummy\project_path.csproj";

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

    [TestMethod]
    public async Task ScanAsync_AddsProject_WhenFileIsEmpty()
    {
        // Arrange
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project = builder.GetOrAddProject(PROJECT_PATH, true);

        // Act
        await scanner.ScanAsync(builder, project, CancellationToken.None);

        // Assert
        Assert.AreEqual("project_path.dll", project.AssemblyName);
        Assert.AreEqual("project_path.pdb", project.PdbFileName);
        Assert.AreEqual(false, project.IsSdk);
        Assert.AreEqual(false, project.IsNetStandard2);
        Assert.AreEqual(true, project.IsPackageRef);
        Assert.AreEqual(0, project.References.Count);
        Assert.AreEqual(0, project.ReferencedBy.Count);
    }

    [TestMethod]
    [DataRow("SampleLib", "Library", "SampleLib.dll", "SampleLib.pdb")]
    [DataRow("SampleApp", "Exe", "SampleApp.exe", "SampleApp.pdb")]
    public async Task ScanAsync_CapturesAssemblyNameAndPdbName_FromXml(string assemblyName, string outputType, string expectedAssemblyName, string expectedPdbFilePath)
    {
        // Arrange
        const string xml = """
                           <AssemblyName>{{ASSEMBLY_NAME}}</AssemblyName>
                           <OutputType>{{OUTPUT_TYPE}}</OutputType>
                           """;
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml.Replace("{{ASSEMBLY_NAME}}", assemblyName).Replace("{{OUTPUT_TYPE}}", outputType));

        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project = builder.GetOrAddProject(PROJECT_PATH, true);

        // Act
        await scanner.ScanAsync(builder, project, CancellationToken.None);

        // Assert
        Assert.AreEqual(expectedAssemblyName, project.AssemblyName);
        Assert.AreEqual(expectedPdbFilePath, project.PdbFileName);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesSdkSettings_FromXml()
    {
        // Arrange
        const string xml = """
                           <Project Sdk="Microsoft.NET.Sdk">
                              <!-- insignificant -->
                           </Project>
                           """;
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project = builder.GetOrAddProject(PROJECT_PATH, true);

        // Act
        await scanner.ScanAsync(builder, project, CancellationToken.None);

        // Assert
        Assert.IsTrue(project.IsSdk);
    }

    [TestMethod]
    [DataRow("NETSTANDARD2.0", true)]
    [DataRow("NET48;NETSTANDARD2.0", true)]
    [DataRow("NETSTANDARD2.0;NET48", true)]
    [DataRow("NET48", false)]
    [DataRow("netstandard2", false)]
    [DataRow("net48;netstandard2", false)]
    [DataRow("netstandard2;net48", false)]
    [DataRow("netstandard2.0", true)]
    [DataRow("net48;netstandard2.0", true)]
    [DataRow("netstandard2.0;net48", true)]
    [DataRow("net48", false)]
    public async Task ScanAsync_CapturesNetStandard2_FromXml(string targetFrameworks, bool expectedResult)
    {
        // Arrange
        var hasMultiple = targetFrameworks.Contains(';');
        var xml = hasMultiple 
            ? $"<TargetFrameworks>{targetFrameworks}</TargetFrameworks>" 
            : $"<TargetFramework>{targetFrameworks}</TargetFramework>";

        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project = builder.GetOrAddProject(PROJECT_PATH, true);

        // Act
        await scanner.ScanAsync(builder, project, CancellationToken.None);

        // Assert
        Assert.AreEqual(expectedResult, project.IsNetStandard2);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesPackageRef_FromXml()
    {
        // Arrange
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.Contains("packages.config")))).Returns(true);
        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project = builder.GetOrAddProject(PROJECT_PATH, true);

        // Act
        await scanner.ScanAsync(builder, project, CancellationToken.None);

        // Assert
        Assert.AreEqual(false, project.IsPackageRef);
    }

    // todo: add reference tests
    [TestMethod]
    public async Task ScanAsync_CapturesProjectReferences_FromXml()
    {
        // Arrange
        const string project2FileName = @"project_path_2.csproj";
        const string project2Path = @"c:\dummy\" + project2FileName;
        const string project1Xml = """
                           <ProjectReference Include="project_path_2.csproj" />
                           """;
        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("csproj")))).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project1Xml);
        _fileStorage.Setup(s => s.ReadAllTextAsync(project2Path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var builder = CreateBuilder();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        var project1 = builder.GetOrAddProject(PROJECT_PATH, true);
        var project2 = builder.GetOrAddProject(project2Path, true);

        // Act
        var a = await scanner.ScanAsync(builder, project1, CancellationToken.None);
        var b = await scanner.ScanAsync(builder, project2, CancellationToken.None);

        // Assert
        Assert.AreSame(project1, a.Project);
        Assert.AreSame(project2, b.Project);
        Assert.AreSame(a.References[0], b.Project);
        Assert.AreEqual(1, a.References.Count);
        Assert.AreEqual(0, b.References.Count);

        Assert.AreEqual(1, project1.References.Count);
        Assert.AreEqual(0, project1.ReferencedBy.Count);
        Assert.AreEqual(project2.Name, project1.References[0]);

        Assert.AreEqual(0, project2.References.Count);
        Assert.AreEqual(1, project2.ReferencedBy.Count);
        Assert.AreEqual(project1.Name, project2.ReferencedBy[0]);

        Assert.AreEqual(0, builder.ProjectFilesToScanCount);
    }

    private BranchDatabaseBuilder CreateBuilder(BuildDefinition[]? builds = null)
    {
        builds ??= [];
        return new BranchDatabaseBuilder(_loggerFactory.Object, _fileStorage.Object, _resolver, builds);
    }
}