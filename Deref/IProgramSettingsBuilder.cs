namespace Deref;

public interface IProgramSettingsBuilder
{
    ProgramSettings Build(string branchName, string customTempDirectoryPath);
}