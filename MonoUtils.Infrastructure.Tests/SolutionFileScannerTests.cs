using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure.FileScanners;
using Moq;

namespace MonoUtils.Infrastructure.Tests;

[TestClass]
public class SolutionFileScannerTests
{
    private const string BUILD_NAME = "test build";
    private const string SOLUTION_PATH = @"c:\dummy\solution_path.sln";
    private const string PROJECT_PATH = @"c:\dummy\project_path.csproj";
    private const string WIX_PROJECT_PATH = @"c:\dummy\wix_project_path.wixproj";

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);

    private Mock<IFileStorage> _fileStorage = null!;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(false);
    }

    [TestMethod]
    public async Task ScanAsync_ShouldReturnEmptyResults_WhenFileDoesNotExist()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(false);
        var scanner = CreateScanner();

        // Act
        var results = await Assert.ThrowsExceptionAsync<FileNotFoundException>(() 
            => scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None));

        // Assert
        Assert.IsTrue(results.Message.Contains("Solution file not found"));
    }

    [TestMethod]
    public async Task ScanAsync_ShouldReturnEmptyResults_WhenFileIsEmpty()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Projects.Count);
        Assert.AreEqual(0, results.WixProjects.Count);
    }

    [TestMethod]
    [DataRow("csproj", true)]
    [DataRow("dbproj", true)]
    [DataRow("sqlproj", true)]
    [DataRow("vbproj", false)]
    [DataRow("proj", false)]
    public async Task ScanAsync_ShouldAddVariousProjectTypes(string projectExtension, bool expectedResult)
    {
        // Arrange
        const string sampleSlnFile =
            """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "project_path.{{EXTENSION}}", "{12345678-1234-1234-1234-123456789012}")
            """;

        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleSlnFile.Replace("{{EXTENSION}}", projectExtension));

        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("proj")))).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("proj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        var expectedCount = expectedResult ? 1 : 0;
        Assert.AreEqual(expectedCount, results.Projects.Count);
    }

    [TestMethod]
    [DataRow("project.cs")]
    [DataRow("project.txt")]
    [DataRow("project.proj")]
    [DataRow("project.xproj")]
    [DataRow("project.ccproj")]
    public async Task ScanAsync_IgnoresNonSupportedProjectTypes(string projectName)
    {
        // Arrange
        const string sampleSlnFile =
            """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "{{PROJECT_NAME}}", "{12345678-1234-1234-1234-123456789012}")
            """;

        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleSlnFile.Replace("{{PROJECT_NAME}}", projectName));

        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("proj")))).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("proj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, results.Projects.Count);
        Assert.AreEqual(0, results.WixProjects.Count);
        Assert.AreEqual(0, results.Errors.Count);
    }

    [TestMethod]
    public async Task ScanAsync_ShouldAddWixProjectTypes()
    {
        // Arrange
        const string sampleSlnFile =
            """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "wix_project_path.wixproj", "{12345678-1234-1234-1234-123456789012}")
            """;

        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleSlnFile);

        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, results.Projects.Count);
        Assert.AreEqual(1, results.WixProjects.Count);
    }

    [TestMethod]
    [DataRow("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", ProjectType.Unknown)]
    [DataRow("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", ProjectType.OldStyle)]
    [DataRow("9A19103F-16F7-4668-BE54-9A1E7A4F7556", ProjectType.SdkStyle)]
    public async Task ScanAsync_ShouldCaptureProjectTypeReferences(string projectGuid, ProjectType expectedType)
    {
        // Arrange
        const string sampleSlnFile =
            """
                Project("{PROJECT_GUID}") = "TestProject", "project_path.csproj", "{12345678-1234-1234-1234-123456789012}")
            """;
        var expectedErrorCount = expectedType == ProjectType.Unknown ? 1 : 0;

        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleSlnFile.Replace("PROJECT_GUID", projectGuid));

        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, results.Projects.Count);
        Assert.AreEqual(PROJECT_PATH, results.Projects[0].Path);
        Assert.AreEqual(expectedType, results.Projects[0].Type);
        Assert.AreEqual(expectedErrorCount, results.Errors.Count);
        Assert.AreEqual(0, results.WixProjects.Count);
    }

    [TestMethod]
    public async Task ScanAsync_ShouldCaptureBothTypesOfProjects()
    {
        // Assemble
        const string sampleSlnFile =
            """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "project_path.csproj", "{12345678-1234-1234-1234-123456789012}")
                Project("{11111111-1111-1111-1111-111111111111}") = "TestWixProject", "wix_project_path.wixproj", "{12345678-1234-1234-1234-123456789112}")
            """;

        _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleSlnFile);

        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("proj")))).Returns(true);
        _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("proj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(SOLUTION_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, results.Projects.Count);
        Assert.AreEqual(1, results.WixProjects.Count);
    }

    private SolutionFileScanner CreateScanner() => new(_fileStorage.Object);
}