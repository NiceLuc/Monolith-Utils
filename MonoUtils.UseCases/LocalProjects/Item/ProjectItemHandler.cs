using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects.Item;

public class ProjectItemHandler(ILogger<ProjectItemHandler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IBranchDatabaseProvider databaseProvider,
        IBranchDatabaseContextFactory contextFactory) : IRequestHandler<ProjectItemQuery, Result<ProjectDTO>>
{
    public async Task<Result<ProjectDTO>> Handle(ProjectItemQuery query, CancellationToken cancellationToken)
    {
        var settings = await settingsBuilder.BuildAsync(cancellationToken);
        var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);
        var context = await contextFactory.CreateAsync(cancellationToken);

        var project = context.GetProject(query.ItemKey!);
        if (project == null)
            return Result<ProjectDTO>.Failure(new Error("Project.NotFound", "Project not found: " + query.ItemKey));

        // var solutions = database.Solutions.ToDictionary(s => s.Name, StringComparer.InvariantCultureIgnoreCase);
        var buildDefinitions = context.GetBuildDefinitionNames(project);

        var result = new ProjectDTO(project.Name, project.Path, project.DoesExist)
        {
            ProjectStatus = GetProjectStatusTerm(project),
            ReferencedByCount = project.ReferencedBy.Count,
            ReferencesCount = project.References.Count,
            WixProjectsCount = project.WixProjects.Count,
            SolutionsCount = project.Solutions.Count,
            BuildDefinitionCount = buildDefinitions?.Length ?? 0,
            Todos = GetTodos(project)
        };

        if (query.IsListReferences)
        {
            var references = context.GetProjectsReferencing(project, query, query.IsRecursive);
            result.References = references.Select(r => new ProjectReferenceDTO(r.Name, r.Path, r.DoesExist)
            {
                IsSdk = r.IsSdk,
                IsNetStandard2 = r.IsNetStandard2,
                IsPackageRef = r.IsPackageRef
            }).ToArray();
        }

        if (query.IsListReferencedBy)
        {
            var referencedBy = context.GetProjectsReferencedBy(project, query);
            result.ReferencedBy = referencedBy.Select(r => new ProjectReferenceDTO(r.Name, r.Path, r.DoesExist)
            {
                IsSdk = r.IsSdk,
                IsNetStandard2 = r.IsNetStandard2,
                IsPackageRef = r.IsPackageRef
            }).ToArray();
        }

        if (query.IsListWixProjects)
        {
            var wixProjects = context.GetWixProjects(project).ToDictionary(w => w.Name);
            result.WixProjects = project.WixProjects.Select(w =>
            {
                var wixProject = wixProjects[w.ProjectName];
                return new ProjectWixReferenceDTO(wixProject.Name, wixProject.Path, wixProject.DoesExist)
                {
                    IsSdk = wixProject.IsSdk,
                    IsHarvested = w.IsHarvested
                };
            }).ToArray();
        }

        if (query.IsListSolutions)
        {
            result.Solutions = project.Solutions.Select(s =>
            {
                var solution = context.GetSolution(s)!;
                return new ProjectSolutionDTO(solution.Name, solution.Path);
            }).ToArray();
        }

        if (query.IsListBuildDefinitions)
        {
            result.BuildNames = buildDefinitions ?? ["None"];
        }

        return Result<ProjectDTO>.Success(result);
    }

    private static string GetProjectStatusTerm(ProjectRecord project)
    {
        return project switch
        {
            {DoesExist: false} => "Missing",
            {IsSdk: true, IsNetStandard2: true, IsPackageRef: true} => "Done",
            _ => "Needs Work"
        };
    }

    private static string[] GetTodos(ProjectRecord project)
    {
        var todos = new List<string>();
        if (!project.IsPackageRef) todos.Add("PackageRef");
        if (!project.IsSdk) todos.Add("SDK upgrade");
        if (!project.IsNetStandard2) todos.Add("NETSTANDARD2");
        if (todos.Count == 0) todos.Add("None");
        return todos.ToArray();
    }
}