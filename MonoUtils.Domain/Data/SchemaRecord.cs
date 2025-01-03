﻿namespace MonoUtils.Domain.Data;

public abstract record SchemaRecord(string Name, string Path, bool IsRequired, bool DoesExist)
{
    public List<string> Errors { get; set; } = new();
}