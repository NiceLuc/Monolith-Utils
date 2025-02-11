using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoUtils.Domain;
using MonoUtils.UseCases.LocalBranches;
using SharedKernel;

namespace Deref.Programs;

public class Branch
{
    public class Request : IRequest<Result>
    {
        public string BranchName { get; set; }
    }

    public class Handler(
        ILogger<Handler> logger,
        IFileStorage fileStorage,
        IDefinitionSerializer<ProgramConfig> configSerializer,
        IOptions<AppSettings> appSettings) : IRequestHandler<Request, Result>
    {
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
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
                    var message = $"No TFS branches found in '{tfsRootPath}'";
                    logger.LogWarning(message);
                    return Result.Failure(BranchErrors.BranchNotFound(message));
                }

                foreach (var branchName in branches)
                {
                    var isCurrent = branchName.Equals(config.BranchName, StringComparison.OrdinalIgnoreCase);
                    var statusChar = isCurrent ? "*" : " ";
                    logger.LogInformation($"{statusChar} {branchName}");
                }

                logger.LogInformation("Use the -s flag to change branches (eg. 'branch -s {{BRANCH_NAME}}')");
                return Result.Success();
            }

            // switch branches
            var branchPath = Path.Combine(tfsRootPath, request.BranchName);
            if (!fileStorage.DirectoryExists(branchPath))
            {
                var message = $"TFS branch does not exist: {branchPath}";
                logger.LogWarning(message);
                return Result.Failure(BranchErrors.BranchNotFound(message));
            }

            if (request.BranchName.Equals(config.BranchName, StringComparison.InvariantCultureIgnoreCase))
            {
                var message = $"Already on '{config.BranchName}' branch";
                logger.LogInformation(message);
                return Result.Failure(BranchErrors.BranchNotChanged(message));
            }

            // persist branch name to file
            config.BranchName = request.BranchName;
            await configSerializer.SerializeAsync(configFilePath, config, cancellationToken);
            logger.LogInformation($"Switched to branch '{config.BranchName}'");
            return Result.Success();
        }
    }
}