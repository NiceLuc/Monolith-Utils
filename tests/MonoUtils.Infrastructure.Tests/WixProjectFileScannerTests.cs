using MonoUtils.Domain;
using MonoUtils.Infrastructure.FileScanners;
using Moq;

namespace MonoUtils.Infrastructure.Tests;

[TestClass]
public class WixProjectFileScannerTests
{
    private const string WIX_PROJECT_DIRECTORY_PATH = @"c:\dummy";
    private const string WIX_PROJECT_PATH = $@"{WIX_PROJECT_DIRECTORY_PATH}\project_path.wixproj";

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);
    private Mock<IFileStorage> _fileStorage = null!;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(true);
        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.Contains("packages.config")))).Returns(false);
    }

    [TestMethod]
    public async Task ScanAsync_ThrowsExceptionWhenFileNotFound()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(false);
        var scanner = CreateScanner();

        // Act
        var error = await Assert.ThrowsExceptionAsync<FileNotFoundException>(() 
            => scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None));

        // Assert
        Assert.IsTrue(error.Message.Contains("WixProject file not found"));
    }

    [TestMethod]
    public async Task ScanAsync_AddsProject_WhenFileIsEmpty()
    {
        // Arrange
        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(false, results.IsSdk);
        Assert.AreEqual(true, results.IsPackageRef);
        Assert.AreEqual(0, results.ProjectReferences.Count);
        Assert.AreEqual(0, results.ComponentFilePaths.Count);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesSdkSettings_FromXml()
    {
        // Arrange
        const string xml =
            """
            <Project Sdk="WixToolset.Sdk/5.0.2">
               <!-- insignificant -->
            </Project>
            """;
        _fileStorage
            .Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);
        _fileStorage
            .Setup(s => s.GetFilePaths(WIX_PROJECT_DIRECTORY_PATH, It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns([]);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.IsTrue(results.IsSdk);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task ScanAsync_CapturesPackageRef(bool containsPackagesConfig, bool expectedResult)
    {
        // Arrange
        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.Contains("packages.config")))).Returns(containsPackagesConfig);
        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(expectedResult, results.IsPackageRef);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesProjectReferences_FromXml()
    {
        // Arrange
        const string project1FileName = "project_path_1.csproj";
        const string project2FileName = "project_path_2.csproj";
        const string wixXml =
            $"""
             <ProjectReference Include="{project1FileName}" />
             <ProjectReference Include="{project2FileName}" />
             """;
        const string project1Path = $@"{WIX_PROJECT_DIRECTORY_PATH}\{project1FileName}";
        const string project2Path = $@"{WIX_PROJECT_DIRECTORY_PATH}\{project2FileName}";

        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wixXml);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, results.ProjectReferences.Count);
        Assert.AreEqual(project1Path, results.ProjectReferences[0]);
        Assert.AreEqual(project2Path, results.ProjectReferences[1]);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesComponentFilePaths_FromNonSdkStyleXml()
    {
        // Arrange
        const string component1FileName = "component1.wxs";
        const string component2FileName = @"nested\component2.wxs";
        const string wixXml =
            $"""
            <Compile Include="{component1FileName}" />
            <Compile Include="{component2FileName}" />
            """;
        const string component1Wxs = $@"{WIX_PROJECT_DIRECTORY_PATH}\{component1FileName}";
        const string component2Wxs = $@"{WIX_PROJECT_DIRECTORY_PATH}\{component2FileName}";

        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wixXml);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, results.ComponentFilePaths.Count);
        Assert.AreEqual(component1Wxs, results.ComponentFilePaths[0]);
        Assert.AreEqual(component2Wxs, results.ComponentFilePaths[1]);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesComponentFilePaths_FromSdkStyleXml()
    {
        // Arrange
        const string component1FileName = "component1.wxs";
        const string component2FileName = @"nested\component2.wxs";
        const string wixXml =
            $"""
             <Project Sdk="WixToolset.Sdk/5.0.2">
                <!-- insignificant -->
             </Project>
             """;
        const string component1Wxs = $@"{WIX_PROJECT_DIRECTORY_PATH}\{component1FileName}";
        const string component2Wxs = $@"{WIX_PROJECT_DIRECTORY_PATH}\{component2FileName}";

        _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wixXml);
        _fileStorage.Setup(s => s.GetFilePaths(WIX_PROJECT_DIRECTORY_PATH, "*.wxs", It.IsAny<SearchOption>()))
            .Returns([component1Wxs, component2Wxs]);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WIX_PROJECT_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, results.ComponentFilePaths.Count);
        Assert.AreEqual(component1Wxs, results.ComponentFilePaths[0]);
        Assert.AreEqual(component2Wxs, results.ComponentFilePaths[1]);
    }

    private WixProjectFileScanner CreateScanner() => new(_fileStorage.Object);
}