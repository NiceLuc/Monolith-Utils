﻿namespace MonoUtils.Domain;

public interface IEmbeddedResourceProvider
{
    Task<string> GetResourceAsStringAsync(string resourceFileName, CancellationToken cancellationToken);
}