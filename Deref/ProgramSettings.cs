using MonoUtils.Domain.Data;

namespace Deref;

public class ProgramSettings
{
    public string BranchName { get; set; }
    public string TfsRootDirectory { get; set; }
    public string TempRootDirectory { get; set; }
    public BuildDefinition[] RequiredBuildSolutions { get; set; }
}