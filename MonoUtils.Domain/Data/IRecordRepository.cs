namespace MonoUtils.Domain.Data;

public interface IRecordRepository<T> where T : SchemaRecord
{
    bool TryGetRecord(string filePath, out T? record);
    T AddRecord(string filePath, bool isRequired);

    T[] GetRecords();
}