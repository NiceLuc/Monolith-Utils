namespace MonoUtils.Domain.Data;

public record ErrorRecord(RecordType RecordType, string RecordName, string Message, ErrorSeverity Severity);