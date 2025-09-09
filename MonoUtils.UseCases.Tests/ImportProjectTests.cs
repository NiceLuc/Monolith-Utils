using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;
using MonoUtils.UseCases.InitializeDatabase;
using Moq;

namespace MonoUtils.UseCases.Tests;

[TestClass]
public class ImportProjectTests
{
    private const string PROJECT_PATH = @"c:\dummy\project_path.csproj";

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);
    private Mock<ISender> _sender = null!;
    private Mock<IFileStorage> _fileStorage = null!;
    private Mock<ILogger<ImportProject.Handler>> _logger = null!;
    private IBranchDatabaseBuilder _builder = null;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(true);

        _sender = _mockRepository.Create<ISender>();
        _logger = _mockRepository.Create<ILogger<ImportProject.Handler>>();

        _builder = TestUtils.CreateBuilder(_fileStorage);
    }


    [TestMethod]
    public async Task Handle_ReturnsProjectRecord_WhenFileDoesNotExist()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var command = new ImportProject.Command { Path = PROJECT_PATH, };
        var handler = CreateHandler();

        // Act
        var results = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsNotNull(results);
        Assert.IsFalse(results.DoesExist);
    }

    [TestMethod]
    public async Task Handle_ReturnsCachedProjectRecord_WhenFileHasAlreadyBeenScanned()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var expected = _builder.GetOrAddProject(PROJECT_PATH);
        var scannedFiles = new ScannedFiles { expected.Path };
        var command = new ImportProject.Command { Path = PROJECT_PATH };
        var handler = CreateHandler(scannedFiles);

        // Act
        var actual = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, scannedFiles.Count);
    }

    [TestMethod]
    public async Task Handle_UpdatesProjectRecord_WhenFileExists()
    {
        // Arrange
        const string xml = """
            <Project Sdk="Microsoft.NET.Sdk">
                <TargetFramework>net48</TargetFramework>
            </Project>
            """;

        var expected = new ProjectRecord("project_path", PROJECT_PATH, true)
        {
            AssemblyName = "project_path.dll",
            PdbFileName = "project_path.pdb",
            IsSdk = true,
            IsPackageRef = false,
            IsNetStandard2 = false,
            IsTestProject = false,
        };

        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        var scanner = new ScannedFiles();
        var handler = CreateHandler(scanner);
        var command = new ImportProject.Command
        {
            Path = PROJECT_PATH,
        };

        // Act
        var actual = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, scanner.Count);
        Assert.AreSame(_builder.GetOrAddProject(PROJECT_PATH), actual);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.Path, actual.Path);
        Assert.AreEqual(expected.DoesExist, actual.DoesExist);
        Assert.AreEqual(expected.AssemblyName, actual.AssemblyName);
        Assert.AreEqual(expected.PdbFileName, actual.PdbFileName);
        Assert.AreEqual(expected.IsSdk, actual.IsSdk);
        Assert.AreEqual(expected.IsPackageRef, actual.IsPackageRef);
        Assert.AreEqual(expected.IsNetStandard2, actual.IsNetStandard2);
        Assert.AreEqual(expected.IsTestProject, actual.IsTestProject);
    }

    [TestMethod]
    public async Task Handle_AddsProjectReference()
    {
        // Arrange
        const string xml = """
            <Project Sdk="Microsoft.NET.Sdk">
                <TargetFramework>net48</TargetFramework>
                <ProjectReference Include="another_project.csproj" />
            </Project>
            """;
        const string another_xml = """
            <Project Sdk="Microsoft.NET.Sdk">
                <TargetFramework>net48</TargetFramework>
            </Project>
            """;

        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("another_project.csproj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(another_xml);

        var scanner = new ScannedFiles();
        var handler = CreateHandler(scanner);

        // set up the project reference record
        var anotherProjectPath = PROJECT_PATH.Replace("project_path", "another_project");
        var anotherProject = new ProjectRecord("another_project", anotherProjectPath, true);
        _sender.Setup(s
            => s.Send(It.Is<ImportProject.Command>(c => c.Path == anotherProjectPath), It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherProject);

        var command = new ImportProject.Command { Path = PROJECT_PATH, };

        // Act
        var actual = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsNotNull(actual);
        var reference = _builder.GetOrAddProject(anotherProjectPath);
        Assert.AreEqual(actual.References[0], reference.Name);
        Assert.AreEqual(reference.ReferencedBy[0], actual.Name);
    }

    /*
    [TestMethod]
    public async Task Handle_AddsProjectReference()
    {
        // Arrange
        const string xml = """
            <Project Sdk="Microsoft.NET.Sdk">
                <TargetFramework>net48</TargetFramework>
                <ProjectReference Include="another_project.csproj" />
            </Project>
            """;

        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        var scanner = new ScannedFiles();
        var handler = CreateHandler(scanner);

        // set up the project reference record
        var anotherProject = new ProjectRecord("test", "test", true);
        _sender.Setup(s
            => s.Send(It.Is<ImportProject.Command>(c
                => c.Path.Contains("another_project")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherProject);

        var command = new ImportProject.Command
        {
            Path = PROJECT_PATH,
        };

        // Act
        var actual = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(actual.References[0], anotherProject.Name);
        Assert.AreEqual(anotherProject.ReferencedBy[0], actual.Name);
    }
    */

    private ImportProject.Handler CreateHandler(ScannedFiles? scannedFiles = null)
    {
        // assemble
        scannedFiles ??= [];
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        return new ImportProject.Handler(_builder, scannedFiles, scanner);
    }
}