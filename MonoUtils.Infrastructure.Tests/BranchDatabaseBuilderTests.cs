using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Infrastructure;
using Moq;
using SharedKernel;

namespace MonoUtils.Infrastructure.Tests
{
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

        #region AddError Tests
        [TestMethod]
        public void AddError_HappyPath()
        {
            // assemble
            var builder = CreateBuilder();

            // act
            builder.AddError("test error message");
            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(result.Errors.Count, 1);
            Assert.AreEqual(result.Errors[0], "test error message");
        }
        #endregion

        #region GetOrAddSolution Tests
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddSolution_CapturesIsRequired(bool isRequired)
        {
            // assemble
            var builds = new List<BuildDefinition>();
            if (isRequired)
                builds.Add(new BuildDefinition(BUILD_NAME, SOLUTION_PATH, true));
            
            var builder = CreateBuilder(builds.ToArray());

            // act
            var solution = builder.GetOrAddSolution(SOLUTION_PATH);
            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(isRequired, solution.IsRequired);
            Assert.IsFalse(solution.DoesExist);
            Assert.AreEqual("solution_path", solution.Name);
            Assert.AreEqual(SOLUTION_PATH, solution.Path);
            Assert.AreEqual(result.Solutions.Count, 1);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddSolution_CapturesDoesExist(bool doesExist)
        {
            // assemble
            _fileStorage.Setup(s => s.FileExists(SOLUTION_PATH)).Returns(doesExist);
            var builder = CreateBuilder();

            // act
            var solution = builder.GetOrAddSolution(SOLUTION_PATH);
            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(doesExist, solution.DoesExist);
            Assert.AreEqual(result.Solutions.Count, 1);
        }

        [TestMethod]
        public void GetOrAddSolution_GetsExistingSolutionReference()
        {
            // assemble
            var builder = CreateBuilder();
            var original = builder.GetOrAddSolution(SOLUTION_PATH);

            // act
            var found = builder.GetOrAddSolution(SOLUTION_PATH);
            var result = builder.CreateDatabase();

            // assert
            Assert.AreSame(original, found);
            Assert.AreEqual(result.Solutions.Count, 1);
        }
        #endregion

        #region GetOrAddProject Tests
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddProject_CapturesIsRequired(bool isRequired)
        {
            // assemble
            var builder = CreateBuilder();

            // act
            var project = builder.GetOrAddProject(PROJECT_PATH, isRequired);
            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(isRequired, project.IsRequired);
            Assert.IsFalse(project.DoesExist);
            Assert.AreEqual("project_path", project.Name);
            Assert.AreEqual(PROJECT_PATH, project.Path);
            Assert.AreEqual(result.Projects.Count, 1);
        }

        [TestMethod]
        [DataRow(true, true, true, true)]
        [DataRow(true, false, true, true)]
        [DataRow(false, true, true, false)]
        [DataRow(false, false, false, true)]
        public void GetOrAddProject_UpdatesIsRequired(bool originalRequired, bool newRequired, bool expectedRequired, bool expectedSame)
        {
            // assemble
            var builder = CreateBuilder();
            var original = builder.GetOrAddProject(PROJECT_PATH, originalRequired);

            // act
            var result = builder.CreateDatabase();
            var project = builder.GetOrAddProject(PROJECT_PATH, newRequired);

            // assert

            if (expectedSame)
                Assert.AreSame(original, project);
            else
                Assert.AreNotSame(original, project);

            Assert.AreEqual(expectedRequired, project.IsRequired);
            Assert.IsFalse(project.DoesExist);
            Assert.AreEqual("project_path", project.Name);
            Assert.AreEqual(PROJECT_PATH, project.Path);
            Assert.AreEqual(result.Projects.Count, 1);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddProject_CapturesDoesExist(bool doesExist)
        {
            // assemble
            _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(doesExist);
            var expectedScanCount = doesExist ? 1 : 0;
            var builder = CreateBuilder();

            // act
            var project = builder.GetOrAddProject(PROJECT_PATH, false);
            var result = builder.CreateDatabase();

            // assert
            Assert.IsFalse(project.IsRequired);
            Assert.AreEqual(doesExist, project.DoesExist);
            Assert.AreEqual("project_path", project.Name);
            Assert.AreEqual(PROJECT_PATH, project.Path);
            Assert.AreEqual(result.Projects.Count, 1);
            Assert.AreEqual(expectedScanCount, builder.ProjectFilesToScanCount);
        }
        #endregion

        #region GetOrAddWiXProject Tests
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddWixProject_CapturesIsRequired(bool isRequired)
        {
            // assemble
            var builder = CreateBuilder();

            // act
            var wix = builder.GetOrAddWixProject(WIX_PROJECT_PATH, isRequired);
            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(isRequired, wix.IsRequired);
            Assert.IsFalse(wix.DoesExist);
            Assert.AreEqual("wix_project_path", wix.Name);
            Assert.AreEqual(WIX_PROJECT_PATH, wix.Path);
            Assert.AreEqual(result.WixProjects.Count, 1);
        }

        [TestMethod]
        [DataRow(true, true, true, true)]
        [DataRow(true, false, true, true)]
        [DataRow(false, true, true, false)]
        [DataRow(false, false, false, true)]
        public void GetOrAddWixProject_UpdatesIsRequired(bool originalRequired, bool newRequired, bool expectedRequired, bool expectedSame)
        {
            // assemble
            var builder = CreateBuilder();
            var original = builder.GetOrAddWixProject(WIX_PROJECT_PATH, originalRequired);

            // act
            var result = builder.CreateDatabase();
            var wix = builder.GetOrAddWixProject(WIX_PROJECT_PATH, newRequired);

            // assert
            if (expectedSame)
                Assert.AreSame(original, wix);
            else
                Assert.AreNotSame(original, wix);

            Assert.AreEqual(expectedRequired, wix.IsRequired);
            Assert.IsFalse(wix.DoesExist);
            Assert.AreEqual("wix_project_path", wix.Name);
            Assert.AreEqual(WIX_PROJECT_PATH, wix.Path);
            Assert.AreEqual(result.WixProjects.Count, 1);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void GetOrAddWixProject_CapturesDoesExist(bool doesExist)
        {
            // assemble
            _fileStorage.Setup(s => s.FileExists(WIX_PROJECT_PATH)).Returns(doesExist);
            var expectedScanCount = doesExist ? 1 : 0;
            var builder = CreateBuilder();

            // act
            var wix = builder.GetOrAddWixProject(WIX_PROJECT_PATH, false);
            var result = builder.CreateDatabase();

            // assert
            Assert.IsFalse(wix.IsRequired);
            Assert.AreEqual(doesExist, wix.DoesExist);
            Assert.AreEqual("wix_project_path", wix.Name);
            Assert.AreEqual(WIX_PROJECT_PATH, wix.Path);
            Assert.AreEqual(result.WixProjects.Count, 1);
            Assert.AreEqual(expectedScanCount, builder.WixProjectFilesToScanCount);
        }
        #endregion

        #region Dynamic Scan Enumeration Tests
        [TestMethod]
        public void GetProjectFilesToScan_SupportsNewProjectsDuringIteration()
        {
            _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(true);
            var builder = CreateBuilder();

            // add initial projects
            for (var index = 0; index < 3; index++) 
                builder.GetOrAddProject(@$"c:\temp\project_{index}_path.csproj", true);

            // act
            var counter = 0;
            foreach (var project in builder.GetProjectFilesToScan())
            {
                Assert.AreEqual($"project_{counter}_path", project.Name);

                // THE TEST: after scanning 2 projects, add a new one!
                if (counter == 1)
                    builder.GetOrAddProject(@"c:\temp\project_3_path.csproj", true);

                counter += 1;
            }

            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(4, result.Projects.Count);
        }

        [TestMethod]
        public void GetWixProjectFilesToScan_SupportsNewWixProjectsDuringIteration()
        {
            _fileStorage.Setup(s => s.FileExists(It.IsAny<string>())).Returns(true);
            var builder = CreateBuilder();

            // add initial projects
            for (var index = 0; index < 3; index++) 
                builder.GetOrAddWixProject(@$"c:\temp\wix_project_{index}_path.csproj", true);

            // act
            var counter = 0;
            foreach (var project in builder.GetWixProjectFilesToScan())
            {
                Assert.AreEqual($"wix_project_{counter}_path", project.Name);

                // THE TEST: after scanning 2 projects, add a new one!
                if (counter == 1)
                    builder.GetOrAddWixProject(@"c:\temp\wix_project_3_path.csproj", true);

                counter += 1;
            }

            var result = builder.CreateDatabase();

            // assert
            Assert.AreEqual(4, result.WixProjects.Count);
        }
        #endregion

        #region GetProjectsBySolutionName Tests

        [TestMethod]
        [Ignore]
        public void GetProjectsBySolutionName_ReturnsEmptyListForUnknownSolution()
        {
            // assemble
            var builder = CreateBuilder();
            var solution = builder.GetOrAddSolution(SOLUTION_PATH);
            var project = builder.GetOrAddProject(PROJECT_PATH, true);
            var result = builder.CreateDatabase();

            // act
        }

        #endregion

        private BranchDatabaseBuilder CreateBuilder(BuildDefinition[]? builds = null)
        {
            builds ??= [];
            return new BranchDatabaseBuilder(_loggerFactory.Object, _fileStorage.Object, _resolver, builds);
        }
    }
}