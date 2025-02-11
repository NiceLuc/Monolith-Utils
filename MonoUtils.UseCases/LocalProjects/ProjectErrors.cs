using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects;

public static class ProjectErrors
{
    public static readonly Error ProjectNotFound = new("Project.NotFound", "ProjectNotFound");
    public static Error InvalidRequest(string message) => new("Project.InvalidRequest", message);
    // etc...
}