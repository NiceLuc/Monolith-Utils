using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using Moq;
using SharedKernel;

namespace MonoUtils.Infrastructure.Tests;

[TestClass]
public class RecordProviderTests
{
    private const string PROJECT_PATH = @"c:\dummy\project.csproj";
    private Mock<IFileStorage> _fileStorage = null!;
    private UniqueNameResolver _resolver = null!;

    [TestInitialize]
    public void BeforeEachTest()
    {
        _fileStorage = new Mock<IFileStorage>(MockBehavior.Strict);
        _resolver = new UniqueNameResolver();
    }


    [TestMethod]
    public void GetOrAdd_RequiresCallback_OnFirstFetch()
    {
        // assemble
        var provider = CreateRecordProvider();

        // act
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            provider.GetOrAdd(PROJECT_PATH));

        // assert
        Assert.IsNotNull(exception);
        Assert.IsTrue(exception.Message.Contains("Factory function must be provided"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetOrAdd_CreatesNewRecord(bool expectedDoesExist)
    {
        // Arrange
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(expectedDoesExist);
        var provider = CreateRecordProvider();

        // Act
        var record = provider.GetOrAdd(PROJECT_PATH, (name, exists) 
            => new ProjectRecord(name, PROJECT_PATH, exists));

        // Assert
        Assert.IsNotNull(record);
        Assert.AreEqual("project", record.Name);
        Assert.AreEqual(PROJECT_PATH, record.Path);
        Assert.AreEqual(expectedDoesExist, record.DoesExist);
        Assert.AreEqual(false, record.IsRequired);
    }

    [TestMethod]
    public void GetOrAdd_GetsExistingRecord()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var provider = CreateRecordProvider();

        // act
        var a = provider.GetOrAdd(PROJECT_PATH, (name, exists) 
            => new ProjectRecord(name, PROJECT_PATH, exists));

        var b = provider.GetOrAdd(PROJECT_PATH);

        // assert
        Assert.IsNotNull(a);
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void GetRecordByPath_ThrowsException_WhenRecordDoesNotExist()
    {
        // assemble
        var provider = CreateRecordProvider();

        // act
        var byPath = Assert.ThrowsException<KeyNotFoundException>(() => provider.GetRecordByPath("missing"));
        var byName = Assert.ThrowsException<KeyNotFoundException>(() => provider.GetRecordByName("missing"));

        // assert
        Assert.IsNotNull(byPath);
        Assert.IsNotNull(byName);
    }

    [TestMethod]
    public void GetOrAdd_CachesRecordByNameAndPath()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var provider = CreateRecordProvider();
        var record = provider.GetOrAdd(PROJECT_PATH, (name, exists)
            => new ProjectRecord(name, PROJECT_PATH, exists));

        // act
        var byPath = provider.GetRecordByPath(record.Path);
        var byName = provider.GetRecordByName(record.Name);

        // assert
        Assert.IsNotNull(byPath);
        Assert.IsNotNull(byName);
        Assert.AreSame(byPath, byName);
    }

    [TestMethod]
    public void UpdateRecord_OverwritesExistingRecord()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var provider = CreateRecordProvider();
        var record = provider.GetOrAdd(PROJECT_PATH, (name, exists)
            => new ProjectRecord(name, PROJECT_PATH, exists));

        // act
        provider.UpdateRecord(record with {IsRequired = true});
        var updatedByPath = provider.GetOrAdd(record.Path);
        var updatedByName = provider.GetRecordByName(record.Name);

        // assert
        Assert.IsNotNull(updatedByPath);
        Assert.AreSame(updatedByPath, updatedByName);
        Assert.AreEqual(true, updatedByPath.IsRequired);
        Assert.AreNotSame(record, updatedByPath);
        Assert.AreNotSame(record, updatedByName);
    }

    [TestMethod]
    public void GetRecords_ReturnsNoRecords()
    {
        // assemble
        var provider = CreateRecordProvider();

        // act
        var records = provider.GetRecords();

        // assert
        Assert.IsNotNull(records);
        Assert.AreEqual(0, records.Count);
    }

    [TestMethod]
    public void GetRecords_ReturnsRecords()
    {
        // assemble
        _fileStorage.Setup(s => s.FileExists(PROJECT_PATH)).Returns(false);
        var provider = CreateRecordProvider();
        provider.GetOrAdd(PROJECT_PATH, (name, exists)
            => new ProjectRecord(name, PROJECT_PATH, exists));

        // act
        var records = provider.GetRecords();

        // assert
        Assert.IsNotNull(records);
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual(PROJECT_PATH, records[0].Path);
    }


    private RecordProvider<ProjectRecord> CreateRecordProvider()
    {
        return new RecordProvider<ProjectRecord>(_resolver, _fileStorage.Object);
    }
}