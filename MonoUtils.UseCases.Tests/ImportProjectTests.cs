using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
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
    private Mock<IFileStorage> _fileStorage = null!;
    private Mock<ILogger<ImportProject.Handler>> _logger;
    private BranchDatabaseBuilder _builder = null!;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = _mockRepository.Create<IFileStorage>();
        _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(false);

        _logger = _mockRepository.Create<ILogger<ImportProject.Handler>>();
    }


    [TestMethod]
    public async Task Handle_ReturnsProjectRecord_WhenFileDoesNotExist()
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var builder = TestUtils.CreateBuilder(_fileStorage);
        var command = new ImportProject.Command
        {
            Path = PROJECT_PATH,
            Builder = builder
        };
        var handler = CreateHandler();

        // Act
        var results = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsNotNull(results);
        Assert.IsFalse(results.DoesExist);
    }


    private ImportProject.Handler CreateHandler(ScannedFiles? scannedFiles = null)
    {
        // assemble
        var sender = _mockRepository.Create<ISender>();
        var scanner = new StandardProjectFileScanner(_fileStorage.Object);
        return new ImportProject.Handler(sender.Object, _logger.Object, scannedFiles ?? [], scanner);
    }
}