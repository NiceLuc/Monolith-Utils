using MonoUtils.Domain.Data;

namespace MonoUtils.Infrastructure;

public interface IBranchDatabaseBuilder
{
    void AddWarning<T>(T record, string message) where T: SchemaRecord;
    void AddError<T>(T record, string message, Exception? exception = null) where T: SchemaRecord;

    void AddBuildSolution(SolutionRecord solution, string buildName);

    SolutionRecord GetOrAddSolution(string solutionPath);
    ProjectRecord GetOrAddProject(string projectPath);
    WixProjectRecord GetOrAddWixProject(string projectPath);

    void AddProjectToSolution(SolutionRecord solution, ProjectRecord project, ProjectType itemType);
    void AddWixProjectToSolution(SolutionRecord solution, WixProjectRecord wixProject);
    void AddProjectReference(ProjectRecord project, ProjectRecord reference);
    void AddWixProjectReference(WixProjectRecord wixProject, ProjectRecord reference, bool isManuallyHarvested);

    void UpdateProject(ProjectRecord project);
    void UpdateWixProject(WixProjectRecord wixProject);
    ProjectRecord[] GetProjectsAvailableForInstallers(SolutionRecord solution);

    BranchDatabase CreateDatabase();
}