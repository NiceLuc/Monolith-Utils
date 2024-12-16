using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoUtils.Domain;

namespace Deref.Programs;

public class Branch
{
    public class Request : IRequest<string>
    {
        public string BranchName { get; set; }
    }

    public class Handler(
        ILogger<Handler> logger,
        IFileStorage fileStorage,
        IDefinitionSerializer<ProgramConfig> configSerializer,
        IOptions<AppSettings> appSettings) : IRequestHandler<Request, string>
    {
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<string> Handle(Request request, CancellationToken cancellationToken)
        {
            var configFilePath = Path.Combine(_appSettings.GetTempPath(), "config.json");
            var config = fileStorage.FileExists(configFilePath)
                ? await configSerializer.DeserializeAsync(configFilePath, cancellationToken)
                : new ProgramConfig {BranchName = _appSettings.DefaultBranchName};

            var tfsRootPath = _appSettings.GetTFSRootPath();

            // list branches
            if (string.IsNullOrEmpty(request.BranchName))
            {
                var branches = fileStorage.GetDirectoryNames(tfsRootPath);
                if (branches.Length == 0)
                {
                    logger.LogWarning($"No TFS branches found in '{tfsRootPath}'");
                    return string.Empty;
                }

                foreach (var branchName in branches)
                {
                    var isCurrent = branchName.Equals(config.BranchName, StringComparison.OrdinalIgnoreCase);
                    var statusChar = isCurrent ? "*" : " ";
                    logger.LogInformation($"{statusChar} {branchName}");
                }

                logger.LogInformation("Use the -s flag to change branches (eg. 'branch -s {{BRANCH_NAME}}')");
                return string.Empty;
            }

            // switch branches
            var branchPath = Path.Combine(tfsRootPath, request.BranchName);
            if (!fileStorage.DirectoryExists(branchPath))
            {
                logger.LogWarning($"TFS branch does not exist: {branchPath}");
                return string.Empty;
            }

            if (request.BranchName.Equals(config.BranchName, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogInformation($"Already on '{config.BranchName}' branch");
                return string.Empty;
            }

            // persist branch name to file
            config.BranchName = request.BranchName;
            await configSerializer.SerializeAsync(configFilePath, config, cancellationToken);
            logger.LogInformation($"Switched to branch '{config.BranchName}'");
            return string.Empty;
        }
    }
}