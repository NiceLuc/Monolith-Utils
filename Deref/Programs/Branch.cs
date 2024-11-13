using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

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
            var tfsRootPath = _appSettings.TFSRootTemplate.Replace("{{BRANCH_NAME}}", "");

            var tempRootPath = _appSettings.TempDirectoryTemplate.Replace("{{BRANCH_NAME}}", "");
            var configFilePath = Path.Combine(tempRootPath, "config.json");
            var config = fileStorage.FileExists(configFilePath)
                ? await configSerializer.DeserializeAsync(configFilePath, cancellationToken)
                : new ProgramConfig {BranchName = _appSettings.DefaultBranchName};

            // list branches
            if (string.IsNullOrEmpty(request.BranchName))
            {
                var branches = fileStorage.GetDirectoryNames(tfsRootPath);
                foreach (var branchName in branches)
                {
                    var isCurrent = branchName == config.BranchName;
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

            config.BranchName = request.BranchName;
            await configSerializer.SerializeAsync(configFilePath, config, cancellationToken);
            logger.LogInformation($"Switched to branch '{config.BranchName}'");
            return string.Empty;
        }
    }
}