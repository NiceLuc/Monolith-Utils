using MonoUtils.Domain;
using MonoUtils.Infrastructure.FileScanners;
using Moq;

namespace MonoUtils.Infrastructure.Tests;

[TestClass]
public class WixComponentFileScannerTests
{
    private const string WXS_ROOT_DIRECTORY_PATH = @"c:\dummy";
    private const string WXS_FILE_PATH = $@"{WXS_ROOT_DIRECTORY_PATH}\component_file.wxs";

    private readonly MockRepository _mockRepository = new(MockBehavior.Strict);
    private Mock<IFileStorage> _fileStorage = null!;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(WXS_FILE_PATH)).Returns(true);
    }

    [TestMethod]
    public async Task ScanAsync_ThrowsExceptionWhenFileNotFound()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(WXS_FILE_PATH)).Returns(false);
        var scanner = CreateScanner();

        // Act
        var error = await Assert.ThrowsExceptionAsync<FileNotFoundException>(() 
            => scanner.ScanAsync(WXS_FILE_PATH, CancellationToken.None));

        // Assert
        Assert.IsTrue(error.Message.Contains("Wxs file not found"));
    }

    [TestMethod]
    public async Task ScanAsync_ReturnsEmptyAssemblyNameList_WhenFileIsEmpty()
    {
        // Arrange
        _fileStorage.Setup(s => s.ReadAllTextAsync(WXS_FILE_PATH, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WXS_FILE_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, results.AssemblyNames.Count);
    }

    [TestMethod]
    public async Task ScanAsync_CapturesAssemblyNames_FromXml()
    {
        // Arrange
        const string assembly1Name = "assembly_1.dll";
        const string assembly2Name = "assembly_2.dll";
        const string wixXml =
            $"""
             <File Source="$(var.Something.TargetPath){assembly1Name}" />
             <File Source="$(var.Something.TargetPath)nested\\{assembly2Name}" />
             """;
        _fileStorage.Setup(s => s.ReadAllTextAsync(WXS_FILE_PATH, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wixXml);

        var scanner = CreateScanner();

        // Act
        var results = await scanner.ScanAsync(WXS_FILE_PATH, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, results.AssemblyNames.Count);
        Assert.AreEqual(assembly1Name, results.AssemblyNames[0]);
        Assert.AreEqual(assembly2Name, results.AssemblyNames[1]);
    }

    private WixComponentFileScanner CreateScanner() => new(_fileStorage.Object);
}