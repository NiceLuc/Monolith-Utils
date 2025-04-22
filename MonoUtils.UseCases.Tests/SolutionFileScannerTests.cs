using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using MonoUtils.Infrastructure.FileScanners;
using Moq;
using SharedKernel;

namespace MonoUtils.UseCases.Tests
{
    [TestClass]
    public class SolutionFileScannerTests
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

        [TestMethod]
        public async Task ScanAsync_ShouldReturnEmptyResults_WhenFileDoesNotExist()
        {
            // Arrange
            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(false);
            var builder = CreateBuilder();
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsNotNull(results.Solution);
            Assert.IsFalse(results.Solution.DoesExist);
            Assert.AreEqual(0, results.Solution.Projects.Count);
            Assert.AreEqual(0, results.Solution.Projects.Count);
            Assert.AreEqual(0, results.WixProjectsToScan.Count);
        }

        [TestMethod]
        public async Task ScanAsync_ShouldReturnEmptyResults_WhenFileIsEmpty()
        {
            // Arrange
            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);
            var builder = CreateBuilder();
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsNotNull(results.Solution);
            Assert.IsTrue(results.Solution.DoesExist);
            Assert.AreEqual(0, results.Solution.Projects.Count);
            Assert.AreEqual(0, results.WixProjectsToScan.Count);
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
            const string sampleSlnFile = """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "project_path.{{EXTENSION}}", "{12345678-1234-1234-1234-123456789012}")
            """;

            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSlnFile.Replace("{{EXTENSION}}", projectExtension));

            _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("proj")))).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("proj")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            var builder = CreateBuilder();
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            var expectedCount = expectedResult ? 1 : 0;
            Assert.AreEqual(expectedCount, results.Solution.Projects.Count);
            Assert.AreEqual(0, results.WixProjectsToScan.Count);
        }

        [TestMethod]
        public async Task ScanAsync_ShouldAddWixProjectTypes()
        {
            // Arrange
            const string sampleSlnFile = """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "wix_project_path.wixproj", "{12345678-1234-1234-1234-123456789012}")
            """;

            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSlnFile);

            _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(WIX_PROJECT_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            var builder = CreateBuilder();
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            Assert.AreEqual(0, results.Solution.Projects.Count);
            Assert.AreEqual(1, results.Solution.WixProjects.Count);
            Assert.AreEqual(1, results.WixProjectsToScan.Count);
        }

        [TestMethod]
        [DataRow("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE", ProjectType.Unknown)]
        [DataRow("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC", ProjectType.OldStyle)]
        [DataRow("9A19103F-16F7-4668-BE54-9A1E7A4F7556", ProjectType.SdkStyle)]
        public async Task ScanAsync_ShouldCaptureProjectTypeReferences(string projectGuid, ProjectType expectedType)
        {
            // Arrange
            const string sampleSlnFile = """
                Project("{PROJECT_GUID}") = "TestProject", "project_path.csproj", "{12345678-1234-1234-1234-123456789012}")
            """;

            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSlnFile.Replace("PROJECT_GUID", projectGuid));

            _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(PROJECT_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            var builder = CreateBuilder();
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            Assert.AreEqual(1, results.Solution.Projects.Count);
            Assert.AreEqual(expectedType, results.Solution.Projects[0].Type);
            Assert.AreEqual(0, results.Solution.WixProjects.Count);
            Assert.AreEqual(0, results.WixProjectsToScan.Count);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ScanAsync_ShouldSetIsRequired_WhenBuildReferencesSolution(bool isRequired)
        {
            // Assemble
            var builds = new List<BuildDefinition>();
            if (isRequired)
                builds.Add(new BuildDefinition(BUILD_NAME, SOLUTION_PATH, true));
            
            const string sampleSlnFile = """
                Project("{00000000-0000-0000-0000-000000000000}") = "TestProject", "project_path.csproj", "{12345678-1234-1234-1234-123456789012}")
                Project("{11111111-1111-1111-1111-111111111111}") = "TestWixProject", "wix_project_path.wixproj", "{12345678-1234-1234-1234-123456789112}")
            """;

            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(SOLUTION_PATH, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSlnFile);

            _fileStorage.Setup(s => s.FileExists(It.Is<string>(x => x.EndsWith("proj")))).Returns(true);
            _fileStorage.Setup(s => s.ReadAllTextAsync(It.Is<string>(x => x.EndsWith("proj")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            var builder = CreateBuilder(builds.ToArray());
            var scanner = new SolutionFileScanner(_fileStorage.Object);

            // Act
            var results = await scanner.ScanAsync(builder, SOLUTION_PATH, CancellationToken.None);

            // Assert
            Assert.AreEqual(1, results.Solution.Projects.Count);
            Assert.AreEqual(1, results.Solution.WixProjects.Count);
            Assert.AreEqual(1, results.WixProjectsToScan.Count);

            Assert.AreEqual(1, builder.ProjectFilesToScanCount);
            Assert.AreEqual(1, builder.WixProjectFilesToScanCount);
            foreach(var project in builder.GetProjectFilesToScan())
                Assert.AreEqual(isRequired, project.IsRequired);
            foreach(var project in builder.GetWixProjectFilesToScan())
                Assert.AreEqual(isRequired, project.IsRequired);

        }

        private BranchDatabaseBuilder CreateBuilder(BuildDefinition[]? builds = null)
        {
            builds ??= [];
            return new BranchDatabaseBuilder(_loggerFactory.Object, _fileStorage.Object, _resolver, builds);
        }
    }
}