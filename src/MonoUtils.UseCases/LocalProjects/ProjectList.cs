using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using MonoUtils.Domain;
using MonoUtils.Domain.Data;
using MonoUtils.Domain.Data.Queries;
using SharedKernel;

namespace MonoUtils.UseCases.LocalProjects;

public static class ProjectList
{
    public class Query : ListQuery, IRequest<Result>
    {
        public TodoFilterType TodoFilter { get; set; }
    }

    public class Handler(
        ILogger<Handler> logger,
        IProgramSettingsBuilder settingsBuilder,
        IBranchDatabaseProvider databaseProvider) : IRequestHandler<Query, Result>
    {
        private static readonly string _separator = new('-', 50);
        private const string _termPattern = "{0,4}"; // right align terms
        private const string _todoPattern = "        - {0}"; // 8 leading spaces!

        public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
        {
            if (!ValidateRequest(query, out var message))
                return Result.Failure(ProjectErrors.InvalidRequest(message));

            var settings = await settingsBuilder.BuildAsync(cancellationToken);
            var database = await databaseProvider.GetDatabaseAsync(settings.BranchName, cancellationToken);

            // list by a string filter
            var prefix = "Listing";
            var projects = database.Projects.AsQueryable();

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                prefix = "Search found";
                projects = projects.Where(p => FilterByName(query.SearchTerm, p));
            }

            var results = projects
                .Where(p => FilterByTests(query.IsExcludeTests, p))
                .Where(p => FilterByTodos(query.TodoFilter, p))
                .Where(p => FilterByRequired(query.BranchFilter, p))
                .ToArray();

            ShowProjectsList(prefix, results, query);
            return Result.Success();
        }


        private static bool FilterByName(string searchTerm, ProjectRecord project) => 
            project.Name.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);

        private static bool FilterByTests(bool isExcludeTests, ProjectRecord project) => 
            !isExcludeTests || !project.IsTestProject;

        private static bool FilterByRequired(FilterType filter, ProjectRecord project) =>
            filter switch
            {
                FilterType.All => true,
                FilterType.OnlyRequired => project.IsRequired,
                FilterType.OnlyNonRequired => !project.IsRequired,
                _ => throw new InvalidOperationException("Invalid filter: " + filter)
            };

        private static bool FilterByTodos(TodoFilterType todo, ProjectRecord project) =>
            todo switch
            {
                TodoFilterType.NoFilter => true,
                TodoFilterType.SdkProjects => !project.IsSdk,
                TodoFilterType.PackageRefs => !project.IsPackageRef,
                TodoFilterType.NetStandard2 => !project.IsNetStandard2,
                _ => !GetProjectStatus(project) // only show those that need ALL updates!
            };

        private static bool ValidateRequest(Query request, out string message)
        {
            message = string.Empty;
            return true;
        }

        private void ShowProjectsList(string prefix, ProjectRecord[] projects, Query query)
        {
            if (projects.Length == 0)
            {
                logger.LogInformation($"{prefix} 0 projects");
            }
            else
            {
                logger.LogInformation($"{prefix} {projects.Length} projects: {IncludeCountsHeader(query)}");

                foreach (var project in projects.OrderBy(p => p.Name)) 
                    ShowProjectDetailsRow(project, query);
            }

            logger.LogInformation(_separator);
        }

        private static string IncludeCountsHeader(Query query) 
            => query.ShowListCounts ? "(uses / used by)" : "";

        private void ShowProjectDetailsRow(ProjectRecord project, Query query)
        {
            var stringBuilder = new StringBuilder();
            var glyph = GetProjectStatusTerm(project);
            stringBuilder.Append($" {glyph} - {project.Name}");

            if (!project.DoesExist)
            {
                stringBuilder.Append(" (missing)");
                logger.LogInformation(stringBuilder.ToString());
                return;
            }

            if (query.ShowListCounts)
                stringBuilder.Append($" ({project.References.Length} / {project.ReferencedBy.Length})");

            logger.LogInformation(stringBuilder.ToString());

            if (query.ShowListTodos)
            {
                if (!project.IsPackageRef)
                    logger.LogInformation(string.Format(_todoPattern, "Migrate to PackageReferences"));

                if (!project.IsSdk)
                    logger.LogInformation(string.Format(_todoPattern, "Upgrade to SDK style formatting"));

                if (!project.IsNetStandard2)
                    logger.LogInformation(string.Format(_todoPattern, "Support NETSTANDARD2"));
            }
        }

        private static string GetProjectStatusTerm(ProjectRecord project)
        {
            if (!project.DoesExist)
                return string.Format(_termPattern, "x");

            var status = GetProjectStatus(project);
            var term = status ? "ok" : "todo";
            return string.Format(_termPattern, term);
        }

        private static bool GetProjectStatus(ProjectRecord project) 
            => project is {IsSdk: true, IsNetStandard2: true, IsPackageRef: true};

    }
}