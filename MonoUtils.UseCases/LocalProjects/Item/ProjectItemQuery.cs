using MediatR;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects.Item;

public class ProjectItemQuery : ItemQuery<ProjectRecord>, IRequest<Result<ProjectDTO>>
{
    public bool IsListBuildDefinitions { get; set; }
    public bool IsListSolutions { get; set; }
    public bool IsListWixProjects { get; set; }
    public bool IsListReferences { get; set; }
    public bool IsListReferencedBy { get; set; }

    public override bool IsActive(ProjectRecord project)
    {
        if (IsExcludeTests && IsTestProjectPath(project))
            return false;

        return string.IsNullOrEmpty(ListSearchTerm) || IsSearchTermMatch(project, ListSearchTerm);
    }

    private static bool IsTestProjectPath(ProjectRecord project) =>
        project.Path.EndsWith("test.csproj", StringComparison.InvariantCultureIgnoreCase) ||
        project.Path.EndsWith("tests.csproj", StringComparison.InvariantCultureIgnoreCase);

    private static bool IsSearchTermMatch(ProjectRecord project, string searchTerm) =>
        project.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);

}