using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects;

public static class ProjectErrors
{
    public static Error ProjectNotFound(string projectName) => new("Project.NotFound", projectName);
    public static Error InvalidRequest(string message) => new("Project.InvalidRequest", message);
    // etc...
}