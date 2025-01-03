﻿using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;

namespace Deref.Programs;

public class Wix
{
    public class Request : IRequest<string>
    {

    }

    public class Handler(ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IBranchDatabaseProvider databaseProvider) : IRequestHandler<Request, string>
    {
        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);

            var lookup = database.WixProjects.ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

            foreach (var wixProject in database.WixProjects) 
                logger.LogInformation(wixProject.Path);

            return string.Empty;
        }

    }
}